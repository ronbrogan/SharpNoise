﻿using System;
using System.Runtime.Serialization;
using System.Threading;

namespace SharpNoise.Modules
{
    /// <summary>
    /// Noise module that caches the last output value generated by a source
    /// module.
    /// </summary>
    /// <remarks>
    /// If an application passes an input value to the <see cref="GetValue"/> method that
    /// differs from the previously passed-in input value, this noise module
    /// instructs the source module to calculate the output value.  This
    /// value, as well as the ( x, y, z ) coordinates of the input
    /// value, are stored (cached) in this noise module.
    ///
    /// If the application passes an input value to the <see cref="GetValue"/> method
    /// that is equal to the previously passed-in input value, this noise
    /// module returns the cached output value without having the source
    /// module recalculate the output value.
    ///
    /// If a source module changes, the cache must be invalidated 
    /// by calling  <see cref="ResetCache"/>.
    ///
    /// Caching a noise module is useful if it is used as a source module for
    /// multiple noise modules.  If a source module is not cached, the source
    /// module will redundantly calculate the same output value once for each
    /// noise module in which it is included.
    ///
    /// This noise module requires one source module.
    /// </remarks>
    [Serializable]
    public class Cache : Module, IDeserializationCallback
    {
        class CacheEntry
        {
            public double x;
            public double y;
            public double z;
            public double value;
        }

        [NonSerialized]
        ThreadLocal<CacheEntry> localCacheEntry;

        /// <summary>
        /// Gets or sets the first source module
        /// </summary>
        public Module Source0
        {
            get { return SourceModules[0]; }
            set { SourceModules[0] = value; }
        }

        void IDeserializationCallback.OnDeserialization(object sender)
        {
            ResetCache();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Cache()
            : base(1)
        {
            localCacheEntry = new ThreadLocal<CacheEntry>();
        }

        public void ResetCache()
        {
            var oldCacheEntry = localCacheEntry;
            localCacheEntry = new ThreadLocal<CacheEntry>();
            oldCacheEntry?.Dispose();
        }

        /// <summary>
        /// See the documentation on the base class.
        /// <seealso cref="Module"/>
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns>Returns the computed value</returns>
        public override double GetValue(double x, double y, double z)
        {
            CacheEntry cached = localCacheEntry.Value;

            if (cached != null)
            {
                if (cached.x == x && cached.y == y && cached.z == z)
                    return cached.value;
            }
            else
            {
                localCacheEntry.Value = cached = new CacheEntry();
            }

            cached.value = SourceModules[0].GetValue(x, y, z);
            cached.x = x;
            cached.y = y;
            cached.z = z;

            return cached.value;
        }
    }
}
