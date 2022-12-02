namespace PSO
{
    public class RastriginFunction : BasePsoFunction
    {

        private RastriginFunction() :
            this(100, 1000, 0.01, 0.0005, 0.10, 0.45)
        {

        }

        public RastriginFunction(
            int _numberOfAgents,
            int _numberOfGenerations,
            double _velocity,
            double _mutationProbability,
            double _randomnessPriority,
            double _globalBestPriority)
            : base(_numberOfAgents, _numberOfGenerations, _velocity, _mutationProbability, _randomnessPriority, _globalBestPriority)
        {

        }

        internal override BasePsoFunctionAgent GetNewAgent()
        {
            return new RastriginFunctionAgent(3);
        }
    }

    public class RastriginFunctionAgent : BasePsoFunctionAgent
    {
        public RastriginFunctionAgent(int _numberOfDimensions)
            : base(_numberOfDimensions)
        {
        }


        internal override double GetFitness(double[] positions)
        {
            return (10 * positions.Length) + positions.Sum(p => (Math.Pow(p, 2) - (10 * Math.Cos(2 * Math.PI * p))));
        }


        internal override void SetMinMaxDimensionsLimits(int _numberOfDimensions)
        {
            // set limits
            for (int i = 0; i < _numberOfDimensions; i++)
            {
                MinDimensions[i] = -5.12;
                MaxDimensions[i] = 5.12;
            }
        }
    }
}
