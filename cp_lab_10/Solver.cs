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
        // iterations returns number of iterations performed.
        public double[] Solve(double[] X0, double eps, int maxIter, out int iterations)
        {
            if (X0 == null) throw new ArgumentNullException(nameof(X0));
            if (X0.Length < N + 1) throw new ArgumentException("Initial vector must be length N+1 (1-based indexing).", nameof(X0));
            if (eps <= 0) throw new ArgumentOutOfRangeException(nameof(eps));
            if (maxIter <= 0) throw new ArgumentOutOfRangeException(nameof(maxIter));

            double[] X = new double[N + 1];
            for (int i = 1; i <= N; i++) X[i] = X0[i];

            for (int k = 1; k <= maxIter; k++)
            {
                FM(X, F);
                Jacob(X);
                // Dx will be overwritten by Gauss
                for (int i = 1; i <= N; i++) Dx[i] = 0.0;
                Gauss(Ja, F, Dx);

                double dxmax = 0.0;
                for (int i = 1; i <= N; i++)
                {
                    X[i] = X[i] - Dx[i];
                    double ad = Math.Abs(Dx[i]);
                    if (ad > dxmax) dxmax = ad;
                }

                iterations = k;
                if (dxmax < eps) return X;
            }

            iterations = maxIter;
            return X;
        }
    }
}
