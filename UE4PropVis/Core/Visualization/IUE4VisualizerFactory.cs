// Copyright 2017-2018 Cameron Angus. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace UE4PropVis
{
    public interface IUE4VisualizerFactory
    {
        UE4Visualizer CreateVisualizer(DkmVisualizedExpression expression);
    }
}
