using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.lib
{
    class Laplace
    {
        const int DefaultStehfest = 14;
        static double[] V; // Stehfest coefficients 
        static double ln2; // log of 2

        public static void InitStehfest(int N)
        {
            ln2 = Math.Log(2.0);
            int N2 = N / 2;
            int NV = 2 * N2;
            V = new double[NV];
            int sign = 1;
            if ((N2 % 2) != 0)
                sign = -1;
            for (int i = 0; i < NV; i++)
            {
                int kmin = (i + 2) / 2;
                int kmax = i + 1;
                if (kmax > N2)
                    kmax = N2;
                V[i] = 0;
                sign = -sign;
                for (int k = kmin; k <= kmax; k++)
                {
                    V[i] = V[i] + (Math.Pow(k, N2) / Factorial(k)) * (Factorial(2 * k)
                         / Factorial(2 * k - i - 1)) / Factorial(N2 - k)
                         / Factorial(k - 1) / Factorial(i + 1 - k);
                }
                V[i] = sign * V[i];
            }
        }

        public static double InverseTransform(Func<double, double> f, double t)
        {
            double ln2t = ln2 / t;
            double x = 0;
            double y = 0;
            for (int i = 0; i < V.Length; i++)
            {
                x += ln2t;
                y += V[i] * f(x);
            }
            return ln2t * y;
        }

        public static double Transform(Func<double, double> F, double s)
        {
            const int DefaultIntegralN = 5000;
            double du = 0.5 / (double)DefaultIntegralN;
            double y = -F(0) / 2.0;
            double u = 0;
            double limit = 1.0 - 1e-10;
            while (u < limit)
            {
                u += du;
                y += 2.0 * Math.Pow(u, s - 1) * F(-Math.Log(u));
                u += du;
                y += Math.Pow(u, s - 1) * F(-Math.Log(u));
            }
            return 2.0 * y * du / 3.0;
        }

        public static double Factorial(int N)
        {
            double x = 1;
            if (N > 1)
            {
                for (int i = 2; i <= N; i++)
                    x = i * x;
            }
            return x;
        }
    }
}
