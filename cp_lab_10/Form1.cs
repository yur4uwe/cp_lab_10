using System;
using System.Globalization;
using System.Windows.Forms;

namespace cp_lab_10
{
    public partial class Form1 : Form
    {
        public double[] X0;
        public double[] X;

        public Form1()
        {
            InitializeComponent();
            if (inputGridView.ColumnCount == 0)
                inputGridView.Columns.Add("col0", "");
            if (outputGridView.ColumnCount == 0)
                outputGridView.Columns.Add("col0", "");
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

            int colIndex = 0;
            if (inputGridView.ColumnCount == 0)
            {
                MessageBox.Show("No input column available. Please provide initial guesses in the input grid.");
                return;
            }

            int N = inputGridView.ColumnCount;

            X0 = new double[N];
            X = new double[N];

            for (int i = 1; i <= N; i++)
            {
                object cellVal = inputGridView.Rows[i - 1].Cells[colIndex].Value;
                string s = cellVal?.ToString();
                double parsed;
                if (!double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsed))
                {
                    MessageBox.Show($"Invalid input at row {i}. Enter a valid number.");
                    return;
                }
                X0[i] = parsed;
            }

            var solver = new Solver(N);
            int iterations;
            try
            {
                double[] result = solver.Solve(X0, eps, maxIter, out iterations);
                iterLabel.Text = "Iterations elapsed: " + iterations.ToString(CultureInfo.InvariantCulture);

                for (int i = 0; i < N; i++)
                    outputGridView[0, i].Value = result[i + 1].ToString(CultureInfo.InvariantCulture);
                MessageBox.Show("Розв'язок СНР знайдено");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Solver error: " + ex.Message);
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            int n = (int)numericUpDown1.Value;

            inputGridView.RowCount = n;
            outputGridView.RowCount = n;
        }
    }
}
