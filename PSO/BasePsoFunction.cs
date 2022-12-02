

using System.Globalization;

namespace PSO
{
    public abstract class BasePsoFunction
    {
        #region Algorithm variables
        internal int numberOfAgents { get; set; }

        internal int numberOfGenerations { get; set; }

        public double velocity { get; private set; }

        public double[] GlobalBestPositions = Array.Empty<double>();

        public double GlobalBestFitness { get; private set; }

        public double mutationProbability { get; private set; }
        public double randomnessPriority { get; private set; }
        public double globalBestPriority { get; private set; }
        public double privateBestPriority { get { return 1.0 - globalBestPriority - randomnessPriority; } }
        private BasePsoFunctionAgent BestAgent = null;
        public int GenerationWhenBestFitnessWasFound = 0;
        #endregion

        public IList<BasePsoFunctionAgent> Agents;
        private BasePsoFunction() :
            this(100, 1000, 0.01, 0.0005, 0.10, 0.45)
        {

        }

        public BasePsoFunction(
            int _numberOfAgents,
            int _numberOfGenerations,
            double _velocity,
            double _mutationProbability,
            double _randomnessPriority,
            double _globalBestPriority)
        {
            // control sanity in the variables
            numberOfAgents = _numberOfAgents;
            numberOfGenerations = _numberOfGenerations;
            velocity = _velocity;
            mutationProbability = _mutationProbability;
            randomnessPriority = _randomnessPriority;
            globalBestPriority = _globalBestPriority;

        }

        internal abstract BasePsoFunctionAgent GetNewAgent();

        public void InitAgents()
        {
            Agents = new List<BasePsoFunctionAgent>();

            for (int i = 0; i < numberOfAgents; i++)
            {
                BasePsoFunctionAgent newAgent = GetNewAgent();
                newAgent.Name = "Agent " + i.ToString();
                Agents.Add(newAgent);
            }

            BestAgent = Agents.OrderBy(a => a.CurrentFitness).First();
            GlobalBestFitness = BestAgent.CurrentFitness;
            GlobalBestPositions = BestAgent.CurrentPositions;
        }

        public void Run()
        {
            InitAgents();

            for (int i = 0; i < numberOfGenerations; i++)
            {
                Parallel.ForEach(Agents, agent =>
                {
                    agent.MoveToNextPosition(GlobalBestPositions,
                                           privateBestPriority,
                                           globalBestPriority,
                                           randomnessPriority,
                                           mutationProbability,
                                           velocity);

                    BasePsoFunctionAgent bestAgentCandidate = Agents.OrderBy(a => a.CurrentFitness).First();
                    ReplaceBestCandidateIfBetter(bestAgentCandidate, i);
                });

            }

            Console.WriteLine("");
            var f = new NumberFormatInfo { NumberGroupSeparator = " " };
            Console.WriteLine($"Best fitness: {GlobalBestFitness}. Found by {BestAgent.Name}, in generation {GenerationWhenBestFitnessWasFound.ToString("n0", f)} at:");
            for (int i = 0; i < GlobalBestPositions.Length; i++)
                Console.WriteLine($"Dim {i}: {GlobalBestPositions[i]}");
        }
        public void RunGeneration(int generation)
        {
            Parallel.ForEach(Agents, agent =>
            {
                agent.MoveToNextPosition(GlobalBestPositions,
                                       privateBestPriority,
                                       globalBestPriority,
                                       randomnessPriority,
                                       mutationProbability,
                                       velocity);

                BasePsoFunctionAgent bestAgentCandidate = Agents.OrderBy(a => a.CurrentFitness).First();
                ReplaceBestCandidateIfBetter(bestAgentCandidate, generation);
            });
        }

        public void ReplaceBestCandidateIfBetter(BasePsoFunctionAgent candidate, int generation)
        {
            if (candidate.CurrentFitness < GlobalBestFitness)
            {
                this.GlobalBestFitness = candidate.CurrentFitness;
                this.GlobalBestPositions = candidate.CurrentPositions;
                BestAgent = candidate;
                GenerationWhenBestFitnessWasFound = generation;
            }
        }
    }


    public abstract class BasePsoFunctionAgent
    {
        public string Name { get; set; }

        public readonly double[] MinDimensions = Array.Empty<double>();

        public readonly double[] MaxDimensions = Array.Empty<double>();

        public double[] CurrentPositions { get; private set; }

        public double[] PrivateBestPositions { get; private set; }

        public double CurrentFitness { get; private set; }

        private double _privateBestFitness { get; set; }

        internal virtual void SetNewPosition(double[] _newPositions)
        {
            CurrentPositions = _newPositions;

            CurrentFitness = GetFitness(CurrentPositions);

            if (CurrentFitness < _privateBestFitness)
            {
                _privateBestFitness = CurrentFitness;
                PrivateBestPositions = CurrentPositions;
            }
        }

        internal abstract double GetFitness(double[] positions);

        public void MoveToNextPosition(double[] globalBestPositions, double privateBestPriority, double globalBestPriority, double randomnessPriority, double mutationProbability, double velocity)
        {
            double mutationShouldBeApplied = (new Random()).NextDouble();
            if (mutationProbability > mutationShouldBeApplied)
            {
                double[] mutatedPositions = new double[globalBestPositions.Length];
                for (int i = 0; i < mutatedPositions.Length; i++)
                {
                    mutatedPositions[i] = GetRandomPosition(MinDimensions[i], MaxDimensions[i]);
                }

                this.SetNewPosition(mutatedPositions);
            }

            double[] newPositionCandidate = new double[globalBestPositions.Length];

            for (int i = 0; i < globalBestPositions.Length; i++)
            {
                double diff = randomnessPriority * GetRandomPosition(MinDimensions[i], MaxDimensions[i]) +
                              privateBestPriority * (this.PrivateBestPositions[i] - this.CurrentPositions[i]) +
                              globalBestPriority * (globalBestPositions[i] - this.CurrentPositions[i]);

                diff *= velocity;

                newPositionCandidate[i] = this.CurrentPositions[i] + diff;

                if (newPositionCandidate[i] < MinDimensions[i] ||
                    newPositionCandidate[i] > MaxDimensions[i])
                {
                    //Console.WriteLine($"The new position calculated for Agent {this.Name} ({newPositionCandidate[i]}) is outside valid borders of Min: {MinDimensions[i]} & Max: {MaxDimensions[i]}.");
                    //Console.WriteLine("The candidate will therefore not move, on this generation");
                    return;
                }
            }

            SetNewPosition(newPositionCandidate);
        }

        public BasePsoFunctionAgent(int _numberOfDimensions)
        {
            this.Name = String.Empty;
            this.CurrentPositions = new double[_numberOfDimensions];
            MinDimensions = new double[_numberOfDimensions];
            MaxDimensions = new double[_numberOfDimensions];

            // set limits
            SetMinMaxDimensionsLimits(_numberOfDimensions);

            // Init Agent
            double[] newPositions = new double[_numberOfDimensions];
            for (int i = 0; i < _numberOfDimensions; i++)
            {
                newPositions[i] = GetRandomPosition(MinDimensions[i], MaxDimensions[i]);
            }
            SetNewPosition(newPositions);

            PrivateBestPositions = newPositions;
            _privateBestFitness = CurrentFitness;
        }

        internal abstract void SetMinMaxDimensionsLimits(int _numberOfDimensions);

        private double GetRandomPosition(double minPos, double maxPos)
        {
            Random rnd = new Random();
            return rnd.NextDouble() * (maxPos - minPos) + minPos;
        }
    }
}
