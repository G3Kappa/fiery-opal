﻿using ConvNetSharp.Volume;
using System;
using System.Collections.Generic;

namespace ConvNetSharp.Core.Layers
{
    /// <summary>
    ///     Implements LeakyReLU nonlinearity elementwise
    ///     x -> x > 0, x, otherwise alpha * x
    /// </summary>
    public class LeakyReluLayer<T> : LayerBase<T> where T : struct, IEquatable<T>, IFormattable
    {
        public LeakyReluLayer(T alpha)
        {
            this.Alpha = alpha;
        }

        public T Alpha { get; set; }

        public LeakyReluLayer(Dictionary<string, object> data) : base(data)
        {
            this.Alpha = (T)Convert.ChangeType(data["Alpha"], typeof(T));
        }

        public override Dictionary<string, object> GetData()
        {
            var dico = base.GetData();

            dico["Alpha"] = this.Alpha;

            return dico;
        }

        public override void Backward(Volume<T> outputGradient)
        {
            this.OutputActivationGradients = outputGradient;
            this.OutputActivation.DoLeakyReluGradient(this.OutputActivationGradients, this.InputActivationGradients, this.Alpha);
        }

        protected override Volume<T> Forward(Volume<T> input, bool isTraining = false)
        {
            input.DoLeakyRelu(this.OutputActivation, this.Alpha);
            return this.OutputActivation;
        }

        public override void Init(int inputWidth, int inputHeight, int inputDepth)
        {
            base.Init(inputWidth, inputHeight, inputDepth);

            this.OutputDepth = inputDepth;
            this.OutputWidth = inputWidth;
            this.OutputHeight = inputHeight;
        }
    }
}