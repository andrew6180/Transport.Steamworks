using System;
using System.Collections.Generic;

namespace Transport.Steamworks.Helpers
{
    ///////////////////////////////////////////////////////////////////////////////////////////////////////
    // https://stackoverflow.com/questions/268321/bidirectional-1-to-1-dictionary-in-c-sharp
    ///////////////////////////////////////////////////////////////////////////////////////////////////////
    

    /// <summary>
    ///     This is a dictionary guaranteed to have only one of each value and key.
    ///     It may be searched either by TFirst or by TSecond, giving a unique answer because it is 1 to 1.
    /// </summary>
    /// <typeparam name="TFirst">The type of the "key"</typeparam>
    /// <typeparam name="TSecond">The type of the "value"</typeparam>
    public class BiDictionary<TFirst, TSecond>
    {
        private readonly IDictionary<TFirst, TSecond> _firstToSecond = new Dictionary<TFirst, TSecond>();
        private readonly IDictionary<TSecond, TFirst> _secondToFirst = new Dictionary<TSecond, TFirst>();

        /// <summary>
        ///     The number of pairs stored in the dictionary
        /// </summary>
        public int Count => _firstToSecond.Count;

        /// <summary>
        ///     Returns the first to second dictionary.
        ///     Useful for iterating.
        /// </summary>
        /// <returns></returns>
        public IDictionary<TFirst, TSecond> Get()
        {
            return _firstToSecond;
        }

        /// <summary>
        ///     Tries to add the pair to the dictionary.
        ///     Throws an exception if either element is already in the dictionary
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        public void Add(TFirst first, TSecond second)
        {
            lock (_firstToSecond)
            {
                lock (_secondToFirst)
                {
                    if (_firstToSecond.ContainsKey(first) || _secondToFirst.ContainsKey(second))
                        throw new ArgumentException("Duplicate first or second");

                    _firstToSecond.Add(first, second);
                    _secondToFirst.Add(second, first);
                }
            }
        }

        /// <summary>
        ///     Find the TSecond corresponding to the TFirst first
        ///     Throws an exception if first is not in the dictionary.
        /// </summary>
        /// <param name="first">the key to search for</param>
        /// <returns>the value corresponding to first</returns>
        public TSecond GetByFirst(TFirst first)
        {
            lock (_firstToSecond)
            {
                lock (_secondToFirst)
                {
                    if (!_firstToSecond.TryGetValue(first, out var second))
                        throw new ArgumentException("first");

                    return second;
                }
            }
        }

        /// <summary>
        ///     Find the TFirst corresponding to the Second second.
        ///     Throws an exception if second is not in the dictionary.
        /// </summary>
        /// <param name="second">the key to search for</param>
        /// <returns>the value corresponding to second</returns>
        public TFirst GetBySecond(TSecond second)
        {
            lock (_firstToSecond)
            {
                lock (_secondToFirst)
                {
                    if (!_secondToFirst.TryGetValue(second, out var first))
                        throw new ArgumentException("second");

                    return first;
                }
            }
        }


        /// <summary>
        ///     Remove the record containing first.
        ///     If first is not in the dictionary, throws an Exception.
        /// </summary>
        /// <param name="first">the key of the record to delete</param>
        public void RemoveByFirst(TFirst first)
        {
            lock (_firstToSecond)
            {
                lock (_secondToFirst)
                {
                    if (!_firstToSecond.TryGetValue(first, out var second))
                        throw new ArgumentException("first");

                    _firstToSecond.Remove(first);
                    _secondToFirst.Remove(second);
                }
            }
        }

        /// <summary>
        ///     Remove the record containing second.
        ///     If second is not in the dictionary, throws an Exception.
        /// </summary>
        /// <param name="second">the key of the record to delete</param>
        public void RemoveBySecond(TSecond second)
        {
            lock (_firstToSecond)
            {
                lock (_secondToFirst)
                {
                    if (!_secondToFirst.TryGetValue(second, out var first))
                        throw new ArgumentException("second");

                    _secondToFirst.Remove(second);
                    _firstToSecond.Remove(first);
                }
            }
        }

        /// <summary>
        ///     Tries to add the pair to the dictionary.
        ///     Returns false if either element is already in the dictionary
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns>true if successfully added, false if either element are already in the dictionary</returns>
        public bool TryAdd(TFirst first, TSecond second)
        {
            lock (_firstToSecond)
            {
                lock (_secondToFirst)
                {
                    if (_firstToSecond.ContainsKey(first) || _secondToFirst.ContainsKey(second))
                        return false;

                    _firstToSecond.Add(first, second);
                    _secondToFirst.Add(second, first);
                }
            }

            return true;
        }

        /// <summary>
        ///     Find the TSecond corresponding to the TFirst first.
        ///     Returns false if first is not in the dictionary.
        /// </summary>
        /// <param name="first">the key to search for</param>
        /// <param name="second">the corresponding value</param>
        /// <returns>true if first is in the dictionary, false otherwise</returns>
        public bool TryGetByFirst(TFirst first, out TSecond second)
        {
            lock (_firstToSecond) return _firstToSecond.TryGetValue(first, out second);
        }

        /// <summary>
        ///     Find the TFirst corresponding to the TSecond second.
        ///     Returns false if second is not in the dictionary.
        /// </summary>
        /// <param name="second">the key to search for</param>
        /// <param name="first">the corresponding value</param>
        /// <returns>true if second is in the dictionary, false otherwise</returns>
        public bool TryGetBySecond(TSecond second, out TFirst first)
        {
            lock (_secondToFirst) return _secondToFirst.TryGetValue(second, out first);
        }

        /// <summary>
        ///     Remove the record containing first, if there is one.
        /// </summary>
        /// <param name="first"></param>
        /// <returns> If first is not in the dictionary, returns false, otherwise true</returns>
        public bool TryRemoveByFirst(TFirst first)
        {
            lock (_firstToSecond)
            {
                lock (_secondToFirst)
                {
                    if (!_firstToSecond.TryGetValue(first, out var second))
                        return false;

                    _firstToSecond.Remove(first);
                    _secondToFirst.Remove(second);
                    return true;
                }
            }
        }

        /// <summary>
        ///     Remove the record containing second, if there is one.
        /// </summary>
        /// <param name="second"></param>
        /// <returns> If second is not in the dictionary, returns false, otherwise true</returns>
        public bool TryRemoveBySecond(TSecond second)
        {
            lock (_firstToSecond)
            {
                lock (_secondToFirst)
                {
                    if (!_secondToFirst.TryGetValue(second, out var first))
                        return false;


                    _secondToFirst.Remove(second);
                    _firstToSecond.Remove(first);
                    return true;
                }
            }
        }

        /// <summary>
        ///     Removes all items from the dictionary.
        /// </summary>
        public void Clear()
        {
            lock (_firstToSecond)
            {
                lock (_secondToFirst)
                {
                    _firstToSecond.Clear();
                    _secondToFirst.Clear();
                }
            }
        }
    }
}