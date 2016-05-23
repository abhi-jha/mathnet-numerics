﻿using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathNet.Numerics.Optimization.LineSearch
{
    public abstract class WolfeLineSearch
    {
        protected double C1 { get; }
        protected double C2 { get; }
        protected double ParameterTolerance { get; }
        protected int MaximumIterations { get; }

        public WolfeLineSearch(double c1, double c2, double parameterTolerance, int maxIterations = 10)
        {
            if (c1 <= 0)
                throw new ArgumentException(string.Format("c1 {0} should be greater than 0", c1));
            if (c2 <= c1)
                throw new ArgumentException(string.Format("c1 {0} should be less than c2 {1}", c1, c2));
            if (c2 >= 1)
                throw new ArgumentException(string.Format("c2 {0} should be less than 1", c2));

            C1 = c1;
            C2 = c2;
            ParameterTolerance = parameterTolerance;
            MaximumIterations = maxIterations;
        }

        /// <summary>Implemented following http://www.math.washington.edu/~burke/crs/408/lectures/L9-weak-Wolfe.pdf</summary>
        /// <param name="startingPoint">The objective function being optimized, evaluated at the starting point of the search</param>
        /// <param name="searchDirection">Search direction</param>
        /// <param name="initialStep">Initial size of the step in the search direction</param>
        public LineSearchResult FindConformingStep(IObjectiveFunctionEvaluation startingPoint, Vector<double> searchDirection, double initialStep)
        {
            return FindConformingStep(startingPoint, searchDirection, initialStep, double.PositiveInfinity);
        }

        /// <summary></summary>
        /// <param name="startingPoint">The objective function being optimized, evaluated at the starting point of the search</param>
        /// <param name="searchDirection">Search direction</param>
        /// <param name="initialStep">Initial size of the step in the search direction</param>
        /// <param name="upperBound">The upper bound</param>
        public LineSearchResult FindConformingStep(IObjectiveFunctionEvaluation startingPoint, Vector<double> searchDirection, double initialStep, double upperBound)
        {
            ValidateInputArguments(startingPoint, searchDirection, initialStep, upperBound);

            double lowerBound = 0.0;
            double step = initialStep;

            double initialValue = startingPoint.Value;
            Vector<double> initialGradient = startingPoint.Gradient;

            double initialDd = searchDirection * initialGradient;

            IObjectiveFunction objective = startingPoint.CreateNew();
            int ii;
            MinimizationResult.ExitCondition reasonForExit = MinimizationResult.ExitCondition.None;
            for (ii = 0; ii < MaximumIterations; ++ii)
            {
                objective.EvaluateAt(startingPoint.Point + searchDirection * step);
                ValidateGradient(objective);    // Differ! (added)
                ValidateValue(objective);       // Differ! (added)

                double stepDd = searchDirection * objective.Gradient;

                if (objective.Value > initialValue + C1 * step * initialDd)
                {
                    upperBound = step;
                    step = 0.5 * (lowerBound + upperBound);
                }
                else if (WolfeCondition(stepDd,initialDd))   // Differ, weak Wolfe
                {
                    lowerBound = step;
                    step = double.IsPositiveInfinity(upperBound) ? 2 * lowerBound : 0.5 * (lowerBound + upperBound);
                }
                else
                {
                    reasonForExit = WolfeExitCondition; 
                    break;
                }

                if (!double.IsInfinity(upperBound))
                {
                    double maxRelChange = 0.0;
                    for (int jj = 0; jj < objective.Point.Count; ++jj)
                    {
                        double tmp = Math.Abs(searchDirection[jj] * (upperBound - lowerBound)) / Math.Max(Math.Abs(objective.Point[jj]), 1.0);
                        maxRelChange = Math.Max(maxRelChange, tmp);
                    }
                    if (maxRelChange < ParameterTolerance)
                    {
                        reasonForExit = MinimizationResult.ExitCondition.LackOfProgress;
                        break;
                    }
                }
            }

            if (ii == MaximumIterations && Double.IsPositiveInfinity(upperBound))
            {
                throw new MaximumIterationsException(String.Format("Maximum iterations ({0}) reached. Function appears to be unbounded in search direction.", MaximumIterations));
            }

            if (ii == MaximumIterations)
            {
                throw new MaximumIterationsException(String.Format("Maximum iterations ({0}) reached.", MaximumIterations));
            }

            return new LineSearchResult(objective, ii, step, reasonForExit);
        }
        protected abstract MinimizationResult.ExitCondition WolfeExitCondition { get; }

        protected abstract bool WolfeCondition(double stepDd, double initialDd);

        protected virtual void ValidateGradient(IObjectiveFunction objective)
        {
        }
        protected virtual void ValidateValue(IObjectiveFunction objective)
        {
        }

        protected virtual void ValidateInputArguments(IObjectiveFunctionEvaluation startingPoint, Vector<double> searchDirection, double initialStep, double upperBound)
        {

        }
    }

        
    
}
