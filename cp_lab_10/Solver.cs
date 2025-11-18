using System;

namespace cp_lab_10
{
    internal class Solver
    {
        private readonly int N;
        private readonly Func<double[], double>[] funcs;
        private readonly double[,] Ja; // 0-based indexing: [0..N-1,0..N-1]
        private readonly double[] F;   // 0-based [0..N-1]
        private readonly double[] Fp;  // 0-based [0..N-1]
        private readonly double[] Dx;  // 0-based [0..N-1]

        // Expose iteration counts so caller (UI) can compare both methods
        public int LastGaussIterations { get; private set; }
        public int LastLUIterations { get; private set; }

        

        // New ctor accepts an array of function delegates; dimension is derived from its length
        public Solver(Func<double[], double>[] functions)
        {
            if (functions == null) throw new ArgumentNullException(nameof(functions));
            if (functions.Length == 0) throw new ArgumentException("functions must contain at least one entry", nameof(functions));

            funcs = functions;
            N = funcs.Length;
            Ja = new double[N, N];
            F = new double[N];
            Fp = new double[N];
            Dx = new double[N];
        }

        // Problem-specific function vector (0-based indexing) - now driven by funcs[]
        private void FM(double[] X, double[] f)
        {
            if (X == null) throw new ArgumentNullException(nameof(X));
            if (f == null) throw new ArgumentNullException(nameof(f));
            if (X.Length < N) throw new ArgumentException("X length must be >= N", nameof(X));
            if (f.Length < N) throw new ArgumentException("f length must be >= N", nameof(f));

            for (int i = 0; i < N; i++)
            {
                // each delegate computes f_i(X)
                f[i] = funcs[i](X);
            }
        }

        // Numerical Jacobian by finite difference (fills instance Ja and returns it) - 0-based
        // NOTE: use a copy of X to avoid mutating caller's array
        private double[,] Jacob(double[] X)
        {
            if (X == null) throw new ArgumentNullException(nameof(X));
            if (X.Length < N) throw new ArgumentException("X length must be >= N", nameof(X));

            double[] Xcopy = (double[])X.Clone();

            FM(Xcopy, F);
            double h = 1e-6;
            for (int j = 0; j < N; j++)
            {
                Xcopy[j] += h;
                FM(Xcopy, Fp);
                for (int i = 0; i < N; i++)
                    Ja[i, j] = (Fp[i] - F[i]) / h;
                Xcopy[j] -= h;
            }
            return Ja;
        }

        // LUDecomposition, Gauss and Solve unchanged in external behavior but use the new FM/Jacob implementation.
        // (LUDecomposition and Gauss implementations retained from previous version)
        public double[] LUDecomposition(double[,] A, double[] b, out double[,] C)
        {
            if (A == null) throw new ArgumentNullException(nameof(A));
            if (b == null) throw new ArgumentNullException(nameof(b));

            int n = A.GetLength(0);
            if (A.GetLength(1) != n) throw new ArgumentException("Matrix A must be square.", nameof(A));
            if (b.Length != n) throw new ArgumentException("Vector b length must match matrix dimension.", nameof(b));

            double[,] L = new double[n, n];
            double[,] U = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = i; j < n; j++)
                {
                    double sum = 0.0;
                    for (int k = 0; k < i; k++)
                        sum += L[i, k] * U[k, j];

                    U[i, j] = A[i, j] - sum;
                }

                if (Math.Abs(U[i, i]) < 1e-12)
                    throw new InvalidOperationException($"Zero (or nearly zero) pivot encountered at U[{i},{i}]. Matrix may be singular or require pivoting.");

                L[i, i] = 1.0;
                for (int j = i + 1; j < n; j++)
                {
                    double sum = 0.0;
                    for (int k = 0; k < i; k++)
                        sum += L[j, k] * U[k, i];

                    L[j, i] = (A[j, i] - sum) / U[i, i];
                }
            }

            C = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (j >= i) C[i, j] = U[i, j];
                    else C[i, j] = L[i, j];
                }
            }

            double[] y = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0.0;
                for (int k = 0; k < i; k++)
                    sum += L[i, k] * y[k];
                y[i] = b[i] - sum;
            }

            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0.0;
                for (int k = i + 1; k < n; k++)
                    sum += U[i, k] * x[k];

                x[i] = (y[i] - sum) / U[i, i];
            }

            return x;
        }

        private void Gauss(double[,] A, double[] b, double[] x)
        {
            if (A == null) throw new ArgumentNullException(nameof(A));
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (x == null) throw new ArgumentNullException(nameof(x));

            int n = A.GetLength(0);
            if (A.GetLength(1) != n) throw new ArgumentException("Matrix A must be square.", nameof(A));
            if (b.Length != n) throw new ArgumentException("Vector b length must match matrix dimension.", nameof(b));
            if (x.Length != n) throw new ArgumentException("Vector x length must match matrix dimension.", nameof(x));

            double[,] Ab = new double[n, n + 1];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    Ab[i, j] = A[i, j];
                Ab[i, n] = b[i];
            }

            for (int i = 0; i < n; i++)
            {
                int maxRow = i;
                for (int k = i + 1; k < n; k++)
                {
                    if (Math.Abs(Ab[k, i]) > Math.Abs(Ab[maxRow, i]))
                        maxRow = k;
                }

                if (maxRow != i)
                {
                    for (int j = i; j <= n; j++)
                    {
                        double tmp = Ab[i, j];
                        Ab[i, j] = Ab[maxRow, j];
                        Ab[maxRow, j] = tmp;
                    }
                }

                if (Math.Abs(Ab[i, i]) < 1e-12)
                    throw new InvalidOperationException($"Zero (or nearly zero) pivot encountered at row {i}. Matrix may be singular or require different pivoting.");

                for (int k = i + 1; k < n; k++)
                {
                    double factor = Ab[k, i] / Ab[i, i];
                    for (int j = i; j <= n; j++)
                        Ab[k, j] -= factor * Ab[i, j];
                }
            }

            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0.0;
                for (int j = i + 1; j < n; j++)
                    sum += Ab[i, j] * x[j];
                if (Math.Abs(Ab[i, i]) < 1e-12)
                    throw new InvalidOperationException($"Zero (or nearly zero) pivot encountered at back-substitution row {i}.");
                x[i] = (Ab[i, n] - sum) / Ab[i, i];
            }
        }

        // Newton method driver
        public double[] Solve(double[] X0, double eps, int maxIter)
        {
            if (X0 == null) throw new ArgumentNullException(nameof(X0));
            if (X0.Length != N) throw new ArgumentException("Initial vector must be length N (0-based indexing).", nameof(X0));
            if (eps <= 0) throw new ArgumentOutOfRangeException(nameof(eps));
            if (maxIter <= 0) throw new ArgumentOutOfRangeException(nameof(maxIter));

            // --- 1) Original approach: recompute Jacob each iteration and solve with Gauss ---
            LastGaussIterations = 0;
            double[] Xgauss = (double[])X0.Clone();

            for (int k = 1; k <= maxIter; k++)
            {
                FM(Xgauss, F);
                Jacob(Xgauss);
                double[,] JaCopy = (double[,])Ja.Clone();

                for (int i = 0; i < N; i++) Dx[i] = 0.0;
                Gauss(JaCopy, F, Dx);

                double dxmax = 0.0;
                for (int i = 0; i < N; i++)
                {
                    Xgauss[i] = Xgauss[i] - Dx[i];
                    double ad = Math.Abs(Dx[i]);
                    if (ad > dxmax) dxmax = ad;
                }

                LastGaussIterations = k;
                if (dxmax < eps)
                    break;
            }

            // --- 2) LU-based approach: compute Jacobian once (at X0), decompose once, then reuse decomposition each iteration ---
            LastLUIterations = 0;
            try
            {
                double[] Xcopy = new double[N];
                for (int i = 0; i < N; i++) Xcopy[i] = X0[i];

                Jacob(Xcopy); // fills Ja and F for Xcopy

                int n = N;
                double[,] JaCopy = (double[,])Ja.Clone();

                double[] b0 = (double[])F.Clone();

                LUDecomposition(JaCopy, b0, out double[,] C);

                double[] Xlu = new double[N];
                for (int i = 0; i < N; i++) Xlu[i] = X0[i];

                for (int k = 1; k <= maxIter; k++)
                {
                    FM(Xlu, F);

                    double[] y = new double[n];
                    for (int i = 0; i < n; i++)
                    {
                        double sum = 0.0;
                        for (int j = 0; j < i; j++)
                            sum += C[i, j] * y[j];
                        y[i] = F[i] - sum;
                    }

                    double[] x0 = new double[n];
                    for (int i = n - 1; i >= 0; i--)
                    {
                        double sum = 0.0;
                        for (int j = i + 1; j < n; j++)
                            sum += C[i, j] * x0[j];
                        if (Math.Abs(C[i, i]) < 1e-12)
                            throw new InvalidOperationException($"Zero (or nearly zero) pivot encountered at U[{i},{i}] during LU solve.");
                        x0[i] = (y[i] - sum) / C[i, i];
                    }

                    double dxmax = 0.0;
                    for (int i = 0; i < n; i++)
                    {
                        Dx[i] = x0[i];
                        Xlu[i] = Xlu[i] - Dx[i];
                        double ad = Math.Abs(Dx[i]);
                        if (ad > dxmax) dxmax = ad;
                    }

                    LastLUIterations = k;
                    if (dxmax < eps)
                        break;
                }
            }
            catch
            {
                LastLUIterations = -1;
            }

            return Xgauss;
        }
    }
}
