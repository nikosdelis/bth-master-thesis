using PSO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SimpleConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            List<Task<psoParams>> tasks = new List<Task<psoParams>>();

            Stopwatch sw = Stopwatch.StartNew();


            for (int i = 0; i < 100; i++)
            {
                string name = "combo_" + i.ToString();
                double prio = 1.0 - Random.Shared.NextDouble();
                prio -= 0.10;
                if (prio < 0) prio = 0;


                tasks.Add(Task.Run(async () => { return RunRastrigin(new psoParams(name, 100, 10000, 0.005, 0.0005, 0.10, prio)); }));
            }

            

            await Task.WhenAll(tasks);

            sw.Stop();

            Console.WriteLine("");
            Console.WriteLine("Elapsed: " + sw.Elapsed.ToString());
            Console.WriteLine("");

            foreach (var task in tasks.OrderBy(t => t.Result.Fitness).Take(5))
                Console.WriteLine($"{task.Result.Name} with GlobalBestPrio {task.Result.GlobalBestPriority} gave a fitness of {task.Result.Fitness}");


            Console.ReadKey();
        }

        public static psoParams RunRastrigin(psoParams p)
        {
            Console.WriteLine("Starting " + p.Name);

            RastriginFunction rf = new RastriginFunction(p.Agents, p.Generations, p.Velocity, p.MutationProbability, p.RandomnessPriority, p.GlobalBestPriority);

            rf.Run();

            p.Fitness = rf.GlobalBestFitness;

            return p;
        }


        public class psoParams
        {
            public psoParams(string name, int agents, int generations, double velocity, double mutationProbability, double randomnessWeight, double globalBestWeight)
            {
                Name = name;
                Agents = agents;
                Generations = generations;
                Velocity = velocity;
                MutationProbability = mutationProbability;
                RandomnessPriority = randomnessWeight;
                GlobalBestPriority = globalBestWeight;
            }

            public string Name { get; set; }
            public int Agents { get; set; }
            public int Generations { get; set; }
            public double Velocity { get; set; }
            public double MutationProbability { get; set; }
            public double RandomnessPriority { get; set; }
            public double GlobalBestPriority { get; set; }

            public double Fitness { get; set; }
        }
    }
}