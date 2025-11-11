using System;
using System.Globalization;
using System.Windows.Forms;

namespace cp_lab_10
{
    public partial class Form1 : Form
    {
        Func<double[], double>[] funcs = new Func<double[], double>[]
        {
            // f1(x) = x1 + exp(x1-1) + (x2+x3)^2 - 27
            vec => vec[0] + Math.Exp(vec[0] - 1.0) + Math.Pow(vec[1] + vec[2], 2) - 27.0,
            // f2(x) = x1 * exp(x2-2) + x3^2 - 10
            vec => vec[0] * Math.Exp(vec[1] - 2.0) + vec[2] * vec[2] - 10.0,
            // f3(x) = x3 + sin(x2-2) + x2^2 - 7
            vec => vec[2] + Math.Sin(vec[1] - 2.0) + vec[1] * vec[1] - 7.0
        };

        public Form1()
        {
            InitializeComponent();

            inputGridView.RowCount = funcs.Length;
            outputGridView.RowCount = funcs.Length;
        }

        private void solveButton_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(epsTextBox.Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double eps) || eps <= 0)
            {
                eps = 1e-6;
                epsTextBox.Text = eps.ToString(CultureInfo.InvariantCulture);
            }
            if (!int.TryParse(maxIterTextBox.Text, out int maxIter) || maxIter <= 0)
            {
                maxIter = 1000;
                maxIterTextBox.Text = maxIter.ToString(CultureInfo.InvariantCulture);
            }

            int N = funcs.Length;

            int colIndex = 0;
            if (inputGridView.ColumnCount == 0)
            {
                MessageBox.Show("No input column available. Please provide initial guesses in the input grid.");
                return;
            }

            var X0 = new double[N];
            var X = new double[N];

            for (int i = 0; i < N; i++)
            {
                object cellVal = inputGridView.Rows[i].Cells[colIndex].Value;
                string s = cellVal?.ToString();
                if (!double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsed))
                {
                    MessageBox.Show($"Invalid input at row {i + 1}. Enter a valid number.");
                    return;
                }
                X0[i] = parsed;
            }

            var solver = new Solver(funcs);
            try
            {
                double[] result = solver.Solve(X0, eps, maxIter);

                var newtonIter = solver.LastGaussIterations;
                var luIter = solver.LastLUIterations;

                iterLabel.Text = "Iterations elapsed for Newton: " + newtonIter.ToString(CultureInfo.InvariantCulture) +
                    "  LU reuse: " + luIter.ToString(CultureInfo.InvariantCulture);

                for (int i = 0; i < N; i++)
                    outputGridView.Rows[i].Cells[colIndex].Value = result[i].ToString(CultureInfo.InvariantCulture);

                MessageBox.Show("Розв'язок СНР знайдено");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Solver error: " + ex.Message);
            }
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            inputGridView.Columns.Clear();
            inputGridView.RowCount = funcs.Length;
            outputGridView.Columns.Clear();
            outputGridView.RowCount = funcs.Length;

            epsTextBox.Clear();
            maxIterTextBox.Clear();
            iterLabel.Text = string.Empty;
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
