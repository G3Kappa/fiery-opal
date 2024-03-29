﻿using ConvNetSharp.Core.Serialization;
using ConvNetSharp.Volume;
using System;
using System.Collections.Generic;

namespace ConvNetSharp.Core.Layers
{
    public class FullyConnLayer<T> : LayerBase<T>, IDotProductLayer<T> where T : struct, IEquatable<T>, IFormattable
    {
        private T _biasPref;

        public FullyConnLayer(Dictionary<string, object> data) : base(data)
        {
            this.L1DecayMul = Ops<T>.Zero;
            this.L2DecayMul = Ops<T>.One;

            this.NeuronCount = Convert.ToInt32(data["NeuronCount"]);
            this.Filters = BuilderInstance<T>.Volume.From(data["Filters"].ToArrayOfT<T>(), new Shape(1, 1, this.InputWidth * this.InputHeight * this.InputDepth, this.NeuronCount));
            this.Bias = BuilderInstance<T>.Volume.From(data["Bias"].ToArrayOfT<T>(), new Shape(1, 1, this.NeuronCount));
            this.BiasPref = (T)Convert.ChangeType(data["BiasPref"], typeof(T));
            this.BiasGradient = BuilderInstance<T>.Volume.From(data["BiasGradient"].ToArrayOfT<T>(), new Shape(1, 1, this.NeuronCount));
            this.FiltersGradient = BuilderInstance<T>.Volume.From(data["FiltersGradient"].ToArrayOfT<T>(), new Shape(1, 1, this.InputWidth * this.InputHeight * this.InputDepth, this.NeuronCount));
            this.IsInitialized = true;
        }

        public FullyConnLayer(int neuronCount)
        {
            this.NeuronCount = neuronCount;

            this.L1DecayMul = Ops<T>.Zero;
            this.L2DecayMul = Ops<T>.One;
        }

        public Volume<T> Bias { get; private set; }

        public Volume<T> BiasGradient { get; private set; }

        public Volume<T> Filters { get; private set; }

        public Volume<T> FiltersGradient { get; private set; }

        public T L1DecayMul { get; set; }

        public T L2DecayMul { get; set; }

        public int NeuronCount { get; }

        public T BiasPref
        {
            get { return this._biasPref; }
            set
            {
                this._biasPref = value;
                if (this.IsInitialized)
                {
                    UpdateOutputSize();
                }
            }
        }

        public override void Backward(Volume<T> outputGradient)
        {
            this.OutputActivationGradients = outputGradient;

            // compute gradient wrt weights and data
            using (var reshapedInput = this.InputActivation.ReShape(1, 1, -1, this.InputActivation.Shape.Dimensions[3]))
            using (var reshapedInputGradients = this.InputActivationGradients.ReShape(1, 1, -1, this.InputActivationGradients.Shape.Dimensions[3]))
            {
                reshapedInput.ConvolveGradient(
                    this.Filters, this.OutputActivationGradients,
                    reshapedInputGradients, this.FiltersGradient,
                    0, 1);

                this.OutputActivationGradients.BiasGradient(this.BiasGradient);
            }
        }

        protected override Volume<T> Forward(Volume<T> input, bool isTraining = false)
        {
            using (var reshapedInput = input.ReShape(1, 1, -1, input.Shape.Dimensions[3]))
            {
                reshapedInput.DoConvolution(this.Filters, 0, 1, this.OutputActivation);
                this.OutputActivation.DoAdd(this.Bias, this.OutputActivation);
                return this.OutputActivation;
            }
        }

        public override Dictionary<string, object> GetData()
        {
            var dico = base.GetData();

            dico["NeuronCount"] = this.NeuronCount;
            dico["Bias"] = this.Bias.ToArray();
            dico["Filters"] = this.Filters.ToArray();
            dico["BiasPref"] = this.BiasPref;
            dico["FiltersGradient"] = this.FiltersGradient.ToArray();
            dico["BiasGradient"] = this.BiasGradient.ToArray();

            return dico;
        }

        public override List<ParametersAndGradients<T>> GetParametersAndGradients()
        {
            var response = new List<ParametersAndGradients<T>>
            {
                new ParametersAndGradients<T>
                {
                    Volume = this.Filters,
                    Gradient = this.FiltersGradient,
                    L2DecayMul = this.L2DecayMul,
                    L1DecayMul = this.L1DecayMul
                },
                new ParametersAndGradients<T>
                {
                    Volume = this.Bias,
                    Gradient = this.BiasGradient,
                    L1DecayMul = Ops<T>.Zero,
                    L2DecayMul = Ops<T>.Zero
                }
            };

            return response;
        }

        public override void Init(int inputWidth, int inputHeight, int inputDepth)
        {
            base.Init(inputWidth, inputHeight, inputDepth);

            UpdateOutputSize();
        }

        internal void UpdateOutputSize()
        {
            this.OutputWidth = 1;
            this.OutputHeight = 1;
            this.OutputDepth = this.NeuronCount;

            // computed
            var inputCount = this.InputWidth * this.InputHeight * this.InputDepth;

            // Full-connected <-> 1x1 convolution
            var scale = Math.Sqrt(2.0 / inputCount);
            this.Filters = BuilderInstance<T>.Volume.Random(new Shape(1, 1, inputCount, this.NeuronCount), 0, scale);
            this.FiltersGradient = BuilderInstance<T>.Volume.SameAs(new Shape(1, 1, inputCount, this.NeuronCount));
            this.Bias = BuilderInstance<T>.Volume.From(new T[this.NeuronCount].Populate(this.BiasPref), new Shape(1, 1, this.NeuronCount));
            this.BiasGradient = BuilderInstance<T>.Volume.SameAs(new Shape(1, 1, this.NeuronCount));
        }
    }
}