using ConvNetSharp.Volume;
using System;

namespace ConvNetSharp.Core.Layers
{
    public interface ILastLayer<T> where T : struct, IEquatable<T>, IFormattable
    {
        void Backward(Volume<T> y, out T loss);
    }
}