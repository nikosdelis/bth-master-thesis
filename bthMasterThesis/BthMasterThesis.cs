using System.Diagnostics;
using System.Net;
using System.Threading;
using DurableTask.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PSO;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace bthMasterThesis
{
    public class BthMasterThesis
    {
        private readonly ILogger _logger;

        public BthMasterThesis(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BthMasterThesis>();
        }

        [Function("SimpleAzureFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {

            var tasks = new List<Task<psoParams>>();

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                string name = "combo_" + i.ToString();
                double prio = 1.0 - Random.Shared.NextDouble();
                prio -= 0.10;
                if (prio < 0) prio = 0;


                tasks.Add(Task.Run(async () =>
                {
                    return RunRastriginLocal(
                        new psoParams(name, 100, 10000, 0.005, 0.0005, 0.10, prio));
                }));
            }

            await Task.WhenAll(tasks);

            sw.Stop();

            _logger.LogError("");

            foreach (var task in tasks.OrderBy(t => t.Result.Fitness).Take(5))
                _logger.LogError($"{task.Result.Name} with GlobalBestPrio {task.Result.GlobalBestPriority} gave a fitness of {task.Result.Fitness}");

            _logger.LogError("Elapsed: " + sw.Elapsed.ToString());

            var response = req.CreateResponse(HttpStatusCode.OK);

            response.WriteString("Elapsed: " + sw.Elapsed.ToString());

            return response;
        }

        public static psoParams RunRastriginLocal(psoParams p)
        {
            Console.WriteLine("Starting " + p.Name);

            RastriginFunction rf = new RastriginFunction(p.Agents, p.Generations, p.Velocity, p.MutationProbability, p.RandomnessPriority, p.GlobalBestPriority);

            rf.Run();

            p.Fitness = rf.GlobalBestFitness;

            return p;
        }

        [Function("StartDurableFunction")]
        public async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableClientContext durableContext,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(HttpStart));

            string instanceId = await durableContext.Client.ScheduleNewOrchestrationInstanceAsync(nameof(RunOrchestrator));
            logger.LogError("Created new orchestration with instance ID = {instanceId}", instanceId);

            return durableContext.CreateCheckStatusResponse(req, instanceId);
        }




        [Function(nameof(RunOrchestrator))]
        public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger log = context.CreateReplaySafeLogger(_logger);
            log.LogError($"It's hammertime!");

            var tasks = new List<Task<string>>();

            DateTime start = context.CurrentUtcDateTime;

            for (int i = 0; i < 100; i++)
            {
                string name = "combo_" + i.ToString();
                double prio = 1.0 - Random.Shared.NextDouble();
                prio -= 0.10;
                if (prio < 0) prio = 0;


                tasks.Add(context.CallActivityAsync<string>(nameof(RunRastrigin), new psoParams(name, 100, 10000, 0.005, 0.0005, 0.10, prio)));
            }

            await Task.WhenAll(tasks);

            DateTime stop = context.CurrentUtcDateTime;

            log.LogError("");
            log.LogError("Elapsed: " + (stop - start).ToString());

            foreach (var task in tasks.OrderBy(t => DeserializePsoResult(t.Result).Fitness).Take(5))
                log.LogError($"{DeserializePsoResult(task.Result).Name} with GlobalBestPrio {DeserializePsoResult(task.Result).GlobalBestPriority} gave a fitness of {DeserializePsoResult(task.Result).Fitness}");
        }



        [Function(nameof(RunRastrigin))]
        public string RunRastrigin([ActivityTrigger] string s)
        {
            psoParams p = Newtonsoft.Json.JsonConvert.DeserializeObject<psoParams>(s);

            RastriginFunction rf = new RastriginFunction(p.Agents, p.Generations, p.Velocity, p.MutationProbability, p.RandomnessPriority, p.GlobalBestPriority);

            rf.Run();

            p.Fitness = rf.GlobalBestFitness;

            return JsonConvert.SerializeObject(p);
        }

        public static psoParams DeserializePsoResult(string s)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<psoParams>(s);
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
