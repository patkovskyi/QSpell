﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QSet.cs" company="Dmytro Patkovskyi">
//   Dmytro Patkovskyi, released under MIT license
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace QSpell.Playground
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class QSet<T> : IEnumerable<IEnumerable<T>>
    {
        public static QSet<T> Create(IEnumerable<IEnumerable<T>> sequences, IComparer<T> comparer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Will throw an exception if alphabet is not enough.
        /// </summary>
        /// <param name="sequences"></param>
        /// <param name="comparer"></param>
        /// <param name="alphabet"></param>
        /// <returns></returns>
        public static QSet<T> Create(
            IEnumerable<IEnumerable<T>> sequences, IComparer<T> comparer, IEnumerable<T> alphabet)
        {
            throw new NotImplementedException();
        }

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<IEnumerable<T>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        protected IEnumerable<IEnumerable<T>> Enumerate()
        {
            return this;
        }

        #endregion
    }
}