using System;

namespace cp_lab_10
{
    internal class Solver
    {
        private readonly int N;
        private readonly double[,] Ja; // 1-based indexing: [1..N,1..N]
        private readonly double[] F;   // 1-based [1..N]
        private readonly double[] Fp;  // 1-based [1..N]
        private readonly double[] Dx;  // 1-based [1..N]

        // Expose iteration counts so caller (UI) can compare both methods
        public int LastGaussIterations { get; private set; }
        public int LastLUIterations { get; private set; }

        public Solver(int n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
            N = n;
            Ja = new double[N + 1, N + 1];
            F = new double[N + 1];
            Fp = new double[N + 1];
            Dx = new double[N + 1];
        }

        // Problem-specific function vector (1-based indexing)
        private void FM(double[] X, double[] f)
        {
            if (X == null) throw new ArgumentNullException(nameof(X));
            if (f == null) throw new ArgumentNullException(nameof(f));
            f[1] = X[1] + Math.Exp(X[1] - 1.0D) + (X[2] + X[3]) * (X[2] + X[3]) - 27.0D;
            f[2] = X[1] * Math.Exp(X[2] - 2.0D) + X[3] * X[3] - 10.0D;
            f[3] = X[3] + Math.Sin(X[2] - 2.0D) + X[2] * X[2] - 7.0D;
        }

        // Numerical Jacobian by finite difference (fills instance Ja and returns it)
        private double[,] Jacob(double[] X)
        {
            FM(X, F);
            double h = 1e-6;
            for (int j = 1; j <= N; j++)
            {
                X[j] += h;
                FM(X, Fp);
                for (int i = 1; i <= N; i++)
                    Ja[i, j] = (Fp[i] - F[i]) / h;
                X[j] -= h;
            }
            return Ja;
        }

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

        // Gaussian elimination with partial pivoting, 1-based indexing
        // Solves A * x = b and stores result in x (x must be length N+1)
        private void Gauss(double[,] A, double[] b, double[] x)
        {
            if (A == null) throw new ArgumentNullException(nameof(A));
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (x == null) throw new ArgumentNullException(nameof(x));

            // Build augmented matrix Ab sized [1..N, 1..N+1] stored in 0..N for simplicity
            double[,] Ab = new double[N + 1, N + 2];
            for (int i = 1; i <= N; i++)
            {
                for (int j = 1; j <= N; j++)
                    Ab[i, j] = A[i, j];
                Ab[i, N + 1] = b[i];
            }

            for (int i = 1; i <= N; i++)
            {
                int maxRow = i;
                for (int k = i + 1; k <= N; k++)
                    if (Math.Abs(Ab[k, i]) > Math.Abs(Ab[maxRow, i]))
                        maxRow = k;

                if (maxRow != i)
                {
                    for (int j = i; j <= N + 1; j++)
                    {
                        double tmp = Ab[i, j];
                        Ab[i, j] = Ab[maxRow, j];
                        Ab[maxRow, j] = tmp;
                    }
                }

                if (Math.Abs(Ab[i, i]) < 1e-12)
                    throw new InvalidOperationException($"Zero (or nearly zero) pivot encountered at row {i}.");

                for (int k = i + 1; k <= N; k++)
                {
                    double factor = Ab[k, i] / Ab[i, i];
                    for (int j = i; j <= N + 1; j++)
                        Ab[k, j] -= factor * Ab[i, j];
                }
            }

            for (int i = N; i >= 1; i--)
            {
                double sum = 0.0;
                for (int j = i + 1; j <= N; j++)
                    sum += Ab[i, j] * x[j];
                if (Math.Abs(Ab[i, i]) < 1e-12)
                    throw new InvalidOperationException($"Zero (or nearly zero) pivot encountered at back-substitution row {i}.");
                x[i] = (Ab[i, N + 1] - sum) / Ab[i, i];
            }
        }

        // Newton method driver: X0 is 1-based length N+1. Returns solution as 1-based array length N+1.
        // iterations returns number of iterations performed for the Gauss (original) method.
        // Additionally populates LastGaussIterations and LastLUIterations so caller can compare both approaches.
        public double[] Solve(double[] X0, double eps, int maxIter)
        {
            if (X0 == null) throw new ArgumentNullException(nameof(X0));
            if (X0.Length < N) throw new ArgumentException("Initial vector must be length N (0-based indexing).", nameof(X0));
            if (eps <= 0) throw new ArgumentOutOfRangeException(nameof(eps));
            if (maxIter <= 0) throw new ArgumentOutOfRangeException(nameof(maxIter));

            // --- 1) Original approach: recompute Jacob each iteration and solve with Gauss ---
            LastGaussIterations = 0;
            double[] Xgauss = new double[N];
            for (int i = 0; i < N; i++) Xgauss[i] = X0[i];

            for (int k = 0; k < maxIter; k++)
            {
                FM(Xgauss, F);
                Jacob(Xgauss);
                for (int i = 0; i < N; i++) Dx[i] = 0.0;
                Gauss(Ja, F, Dx);

                double dxmax = 0.0;
                for (int i = 0; i < N; i++)
                {
                    Xgauss[i] = Xgauss[i] - Dx[i];
                    double ad = Math.Abs(Dx[i]);
                    if (ad > dxmax) dxmax = ad;
                }

                LastGaussIterations = k;
                if (dxmax < eps)
                {
                    // store iterations in out param and proceed to LU-based comparison (below)
                    // continue to compute LU comparison (do not return yet)
                    goto ComputeLU;
                }
            }
        ComputeLU:
            // --- 2) LU-based approach: compute Jacobian once (at X0), decompose once, then reuse decomposition each iteration ---
            LastLUIterations = 0;
            try
            {
                // Prepare a copy of X0 because Jacob mutates X by adding/subtracting h
                double[] Xcopy = new double[N];
                for (int i = 0; i < N; i++) Xcopy[i] = X0[i];

                // Compute Jacobian at initial approximation X0 => Ja will be filled (1-based)
                Jacob(Xcopy); // fills F as well for Xcopy

                // Convert Ja (1-based) to 0-based A for LUDecomposition
                int n = N;
                double[,] A0 = new double[n, n];
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        A0[i, j] = Ja[i, j];

                // Build initial RHS b0 from F (Jacobian computed above left F filled for Xcopy)
                double[] b0 = new double[n];
                for (int i = 0; i < n; i++) b0[i] = F[i];   

                // Decompose once and receive combined matrix C (lower=L (strict), upper=U)
                double[,] C;
                // LUDecomposition also returns a solution for the provided b0; we only need C,
                // but the method returns it via out parameter.
                // Call it to produce C (and ignore the returned solution).
                LUDecomposition(A0, b0, out C);

                // Now iterate using the fixed decomposition C for different RHS = F(X)
                double[] Xlu = new double[N];
                for (int i = 0; i < N; i++) Xlu[i] = X0[i];

                for (int k = 0; k < maxIter; k++)
                {
                    // compute current RHS F at Xlu
                    FM(Xlu, F);

                    // forward substitution to solve L * y = F  (L has implicit diagonal 1, L entries are C[i,j] for j < i)
                    double[] y = new double[n];
                    for (int i = 0; i < n; i++)
                    {
                        double sum = 0.0;
                        for (int j = 0; j < i; j++)
                            sum += C[i, j] * y[j]; // L[i,j] stored in C when j < i
                        y[i] = F[i + 1] - sum;
                    }

                    // backward substitution to solve U * x = y (U entries are C[i,j] for j >= i)
                    double[] x0 = new double[n];
                    for (int i = n - 1; i >= 0; i--)
                    {
                        double sum = 0.0;
                        for (int j = i + 1; j < n; j++)
                            sum += C[i, j] * x0[j]; // U[i,j] stored in C when j >= i
                        if (Math.Abs(C[i, i]) < 1e-12)
                            throw new InvalidOperationException($"Zero (or nearly zero) pivot encountered at U[{i},{i}] during LU solve.");
                        x0[i] = (y[i] - sum) / C[i, i];
                    }

                    // apply correction and check convergence
                    double dxmax = 0.0;
                    for (int i = 0; i < n; i++)
                    {
                        Dx[i + 1] = x0[i]; // store into 1-based Dx
                        Xlu[i + 1] = Xlu[i + 1] - Dx[i + 1];
                        double ad = Math.Abs(Dx[i + 1]);
                        if (ad > dxmax) dxmax = ad;
                    }

                    LastLUIterations = k;
                    if (dxmax < eps)
                        break;
                }
            }
            catch
            {
                // If LU decomposition or solve fails, mark as -1 to indicate failure
                LastLUIterations = -1;
            }

            // return Gauss-based solution to preserve original behavior, and iterations already set
            return Xgauss;
        }
    }
}
