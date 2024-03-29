﻿using ConvNetSharp.Volume;
using System;

namespace ConvNetSharp.Core.Layers
{
    public class ParametersAndGradients<T> where T : struct, IEquatable<T>, IFormattable
    {
        public Volume<T> Volume { get; set; }

        public Volume<T> Gradient { get; set; }

        public T? L2DecayMul { get; set; }

        public T? L1DecayMul { get; set; }
    }
}