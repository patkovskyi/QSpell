﻿namespace QStore.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using QStore.Core.Comparers;
    using QStore.Core.Extensions;
    using QStore.Core.Interfaces;
    using QStore.Core.Minimization;
    using QStore.Core.Structs;

    public class QSet<T> : ISequenceSet<T>, IEnumerable<T[]>
    {
        protected internal T[] Alphabet;

        protected internal IComparer<T> Comparer;

        protected internal int RootState;

        protected internal int[] StateStarts;

        protected internal QSetTransition[] Transitions;

        public long Count { get; protected internal set; }

        public static QSet<T> Create(IEnumerable<IEnumerable<T>> sequences, IComparer<T> comparer)
        {
            return Create<QSet<T>>(sequences, comparer);
        }

        public bool Contains(IEnumerable<T> sequence)
        {
            QSetTransition transition;
            if (this.TrySendSequence(this.RootState, sequence, out transition))
            {
                return transition.IsFinal;
            }

            return false;
        }

        public IEnumerable<T[]> GetByPrefix(IEnumerable<T> prefix)
        {
            QSetTransition transition;
            var fromStack = new Stack<int>();
            if (this.TrySendSequence(this.RootState, prefix, out transition, fromStack))
            {
                return this.Enumerate(transition.StateIndex, fromStack);
            }

            return Enumerable.Empty<T[]>();
        }

        public IEnumerator<T[]> GetEnumerator()
        {
            return this.Enumerate(this.RootState).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        protected static TSet Create<TSet>(IEnumerable<IEnumerable<T>> sequences, IComparer<T> comparer)
            where TSet : QSet<T>, new()
        {
            // avoid multiple enumeration
            var seqArray = sequences as T[][] ?? sequences.Select(s => s.ToArray()).ToArray();

            T[] alphabet;
            SortedDictionary<T, int> alphabetDict;
            ExtractAlphabet(seqArray, comparer, out alphabet, out alphabetDict);

            // outer list represents states, inner lists represent transitions from this state
            // State1 -------> State2 -------> ...... -------> StateN
            // |               |
            // a->State2       b->State3
            // |               |
            // d->State5       e->State4,final
            // |
            // z->StateN,final
            // capacity is set to alphabet.Length just to avoid few initial resizings
            var transitions = new List<List<QSetTransition>>(alphabet.Length) { new List<QSetTransition>() };

            foreach (var sequence in seqArray)
            {
                int nextState = 0, transitionIndex = -1;
                List<QSetTransition> currentStateTransitions = null;
                foreach (var symbol in sequence)
                {
                    currentStateTransitions = transitions[nextState];
                    transitionIndex = GetTransitionIndex(
                        currentStateTransitions, symbol, alphabet, comparer, 0, currentStateTransitions.Count);
                    if (transitionIndex >= 0)
                    {
                        // transition from currentState by symbol already exists
                        nextState = currentStateTransitions[transitionIndex].StateIndex;
                    }
                    else
                    {
                        // transition from currentState by symbol doesn't exist
                        transitionIndex = ~transitionIndex;

                        // find alphabetIndex for symbol
                        int alphabetIndex = alphabetDict[symbol];

                        // add new state
                        nextState = transitions.Count;
                        transitions.Add(new List<QSetTransition>());

                        // add transition from current state to new state                                                
                        var newTransition = new QSetTransition(alphabetIndex, nextState, false);

                        // I know this Insert in the middle of the List<> doesn't look very clever,
                        // but I tried SortedDictionary and LinkedList approach: they are slower in practice
                        currentStateTransitions.Insert(transitionIndex, newTransition);
                    }
                }

                if (currentStateTransitions == null)
                {
                    throw new ArgumentException(ErrorMessages.EmptySequencesAreNotSupported);
                }

                if (currentStateTransitions[transitionIndex].IsFinal)
                {
                    throw new ArgumentException(
                        string.Format(ErrorMessages.DuplicateKey, string.Concat(sequence.Select(e => e.ToString()))));
                }

                // mark last transition in this sequence as final
                currentStateTransitions[transitionIndex] = currentStateTransitions[transitionIndex].MakeFinal();
            }

            var result = new TSet
            {
                Comparer = comparer, 
                Alphabet = alphabet, 
                RootState = 0, 
                StateStarts = new int[transitions.Count], 
                Transitions = new QSetTransition[transitions.Sum(s => s.Count)], 
                Count = seqArray.Length
            };

            for (int i = 0, transitionIndex = 0; i < transitions.Count; i++)
            {
                result.StateStarts[i] = transitionIndex;
                for (int j = 0; j < transitions[i].Count; j++, transitionIndex++)
                {
                    result.Transitions[transitionIndex] = transitions[i][j];
                }
            }

            result.Minimize();
            return result;
        }

        protected IEnumerable<T[]> Enumerate(int fromState, Stack<int> fromStack = null)
        {
            fromStack = fromStack ?? new Stack<int>();
            var toStack = new Stack<int>();
            int lower = this.StateStarts[fromState];
            int upper = this.Transitions.GetUpperIndex(this.StateStarts, fromState);
            fromStack.Push(lower);
            toStack.Push(upper);

            while (fromStack.Count > 0)
            {
                lower = fromStack.Pop();
                upper = toStack.Peek();

                if (lower < upper)
                {
                    fromStack.Push(lower + 1);

                    if (this.Transitions[lower].IsFinal)
                    {
                        var tmp = new int[fromStack.Count];
                        fromStack.CopyTo(tmp, 0);
                        var res = new T[fromStack.Count];
                        for (int i = 0; i < res.Length; i++)
                        {
                            res[i] = this.Alphabet[this.Transitions[tmp[res.Length - i - 1] - 1].AlphabetIndex];
                        }

                        yield return res;
                    }

                    int nextState = this.Transitions[lower].StateIndex;
                    int nextLower = this.StateStarts[nextState];
                    int nextUpper = this.Transitions.GetUpperIndex(this.StateStarts, nextState);
                    if (nextLower < nextUpper)
                    {
                        fromStack.Push(nextLower);
                        toStack.Push(nextUpper);
                    }
                }
                else
                {
                    toStack.Pop();
                }
            }
        }

        protected bool TrySend(int fromState, T input, out int transitionIndex)
        {
            transitionIndex = this.GetTransitionIndex(fromState, input);
            if (transitionIndex >= 0)
            {
                return true;
            }

            return false;
        }

        protected bool TrySendSequence(
            int fromState, IEnumerable<T> sequence, out QSetTransition transition, Stack<int> nextTransitions = null)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException("sequence");
            }

            int currentState = fromState;
            transition = default(QSetTransition);

            foreach (var element in sequence)
            {
                int transitionIndex;
                if (this.TrySend(currentState, element, out transitionIndex))
                {
                    currentState = this.Transitions[transitionIndex].StateIndex;

                    if (nextTransitions != null)
                    {
                        nextTransitions.Push(transitionIndex + 1);
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static void ExtractAlphabet(
            IEnumerable<IEnumerable<T>> sequences, 
            IComparer<T> comparer, 
            out T[] alphabet, 
            out SortedDictionary<T, int> alphabetDictionary)
        {
            alphabetDictionary = new SortedDictionary<T, int>(comparer);
            foreach (var sequence in sequences)
            {
                foreach (T element in sequence)
                {
                    if (!alphabetDictionary.ContainsKey(element))
                    {
                        alphabetDictionary.Add(element, 0);
                    }
                }
            }

            alphabet = alphabetDictionary.Keys.ToArray();

            for (int i = 0; i < alphabet.Length; i++)
            {
                alphabetDictionary[alphabet[i]] = i;
            }
        }

        private static int GetTransitionIndex(
            IList<QSetTransition> transitions, T symbol, T[] alphabet, IComparer<T> comparer, int lower, int upper)
        {
            const int BinarySearchThreshold = 5;
            if (upper - lower >= BinarySearchThreshold)
            {
                // binary search
                upper--;
                while (lower <= upper)
                {
                    int middle = (lower + upper) / 2;
                    int comparisonResult = comparer.Compare(alphabet[transitions[middle].AlphabetIndex], symbol);
                    if (comparisonResult == 0)
                    {
                        return middle;
                    }

                    if (comparisonResult > 0)
                    {
                        upper = middle - 1;
                    }
                    else
                    {
                        lower = middle + 1;
                    }
                }

                return ~lower;
            }

            // linear search
            for (int i = lower; i < upper; i++)
            {
                int comp = comparer.Compare(alphabet[transitions[i].AlphabetIndex], symbol);
                if (comp == 0)
                {
                    return i;
                }

                if (comp > 0)
                {
                    return ~i;
                }
            }

            return ~upper;
        }

        private MergeList GetMergeList()
        {
            var merge = new MergeList(this.StateStarts.Length, this.Transitions.Length);
            for (int i = 0; i < this.Transitions.Length; i++)
            {
                merge.Add(i, this.Transitions[i].StateIndex);
            }

            return merge;
        }

        /// <returns>Transition index or bitwise complement to the index where it should be inserted.</returns>
        private int GetTransitionIndex(int fromState, T symbol)
        {
            int lower = this.StateStarts[fromState];
            int upper = this.Transitions.GetUpperIndex(this.StateStarts, fromState);
            return GetTransitionIndex(this.Transitions, symbol, this.Alphabet, this.Comparer, lower, upper);
        }

        private void Minimize()
        {
            var registered = new Dictionary<StateSignature, int>(new StateSignatureEqualityComparer());
            var mergeList = this.GetMergeList();
            this.Register(this.RootState, registered, mergeList);

            var registeredArray = registered.ToArray();
            var oldToNewStates = Enumerable.Range(0, registered.Count)
                                           .ToDictionary(i => registeredArray[i].Value, i => i);
            this.Transitions = new QSetTransition[registered.Sum(r => r.Key.Transitions.Length)];
            this.StateStarts = new int[registered.Count];
            for (int i = 0, transIndex = 0; i < this.StateStarts.Length; i++)
            {
                var old = registeredArray[i];
                for (int j = 0; j < old.Key.Transitions.Length; j++)
                {
                    var oldTr = old.Key.Transitions[j];
                    this.Transitions[transIndex + j] = new QSetTransition(
                        oldTr.AlphabetIndex, oldToNewStates[oldTr.StateIndex], oldTr.IsFinal);
                }

                this.StateStarts[i] = transIndex;
                transIndex += old.Key.Transitions.Length;
            }

            this.RootState = oldToNewStates[this.RootState];
        }

        private int Register(int state, Dictionary<StateSignature, int> registered, MergeList mergeList)
        {
            int lower = this.StateStarts[state];
            int upper = this.Transitions.GetUpperIndex(this.StateStarts, state);
            int registeredState;

            for (int i = lower; i < upper; i++)
            {
                int sj = this.Transitions[i].StateIndex;
                registeredState = this.Register(sj, registered, mergeList);
                if (registeredState >= 0)
                {
                    // state sj has to be substituted with registeredState                    
                    foreach (var t in mergeList.GetTransitionIndexes(sj))
                    {
                        var transition = this.Transitions[t];
                        this.Transitions[t] = new QSetTransition(
                            transition.AlphabetIndex, registeredState, transition.IsFinal);
                    }

                    mergeList.Merge(registeredState, sj);
                }
            }

            var sigTransitions = new QSetTransition[upper - lower];
            for (int i = lower; i < upper; i++)
            {
                sigTransitions[i - lower] = this.Transitions[i];
            }

            var signature = new StateSignature(sigTransitions);
            if (registered.TryGetValue(signature, out registeredState))
            {
                return registeredState;
            }

            registered.Add(signature, state);
            return -1;
        }
    }
}