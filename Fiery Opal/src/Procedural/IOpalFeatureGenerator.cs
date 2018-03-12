using System;

namespace FieryOpal.Src.Procedural
{
    public interface IOpalFeatureGenerator : IDisposable
    {
        /// <summary>
        /// Yield the generated tile at coordinates X, Y.
        /// </summary>
        /// <param name="x">The tile's X coordinate.</param>
        /// <param name="y">The tile's Y coordinate.</param>
        /// <returns></returns>
        OpalTile Get(int x, int y);
        /// <summary>
        /// Yield the generated decoration at coordinates X, Y.
        /// </summary>
        /// <param name="x">The decoration's X coordinate.</param>
        /// <param name="y">The decoration's Y coordinate.</param>
        /// <returns></returns>
        IDecoration GetDecoration(int x, int y);
        /// <summary>
        /// Optionally called if the generator must rely on state in order to yield a tile for a given point.
        /// </summary>
        /// <param name="m">The requesting map in its current state.</param>
        void Generate(OpalLocalMap m);
    }
}

