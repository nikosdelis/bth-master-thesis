using System.Globalization;

namespace PSO
{
    public class SphereFunction : BasePsoFunction
    {
        private SphereFunction() :
            this(100, 1000, 0.01, 0.0005, 0.10, 0.45)
        {

        }

        public SphereFunction(
            int _numberOfAgents,
            int _numberOfGenerations,
            double _velocity,
            double _mutationProbability,
            double _randomnessPriority,
            double _globalBestPriority)
            : base(_numberOfAgents, _numberOfGenerations, _velocity, _mutationProbability, _randomnessPriority, _globalBestPriority)
        { }

        internal override BasePsoFunctionAgent GetNewAgent()
        {
            return new SphereFunctionAgent(3);
        }
    }

    public class SphereFunctionAgent : BasePsoFunctionAgent
    {
        public SphereFunctionAgent(int _numberOfDimensions) : base(_numberOfDimensions) { }
        internal override double GetFitness(double[] positions)
        {
            return positions.Sum(p => Math.Pow(p, 2));
        }

        internal override void SetMinMaxDimensionsLimits(int _numberOfDimensions)
        {
            // set limits
            for (int i = 0; i < _numberOfDimensions; i++)
            {
                MinDimensions[i] = -2 * Math.PI;
                MaxDimensions[i] = 2 * Math.PI;
            }
        }
    }
}