using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Castle.ActiveRecord
{
	/// <summary>
	/// Provides the base class for a generic read-only dictionary.
	/// </summary>
	/// <typeparam name="TKey">
	/// The type of keys in the dictionary.
	/// </typeparam>
	/// <typeparam name="TValue">
	/// The type of values in the dictionary.
	/// </typeparam>
	[Serializable]
	[DebuggerDisplay("Count = {Count}")]
	[ComVisible(false)]
	[DebuggerTypeProxy(typeof (ReadOnlyDictionaryDebugView<,>))]
	public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection {
		readonly IDictionary<TKey, TValue> _source;
		object _syncRoot;

		public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionaryToWrap) {
			if (dictionaryToWrap == null) {
				throw new ArgumentNullException("dictionaryToWrap");
			}

			_source = dictionaryToWrap;
		}

		public int Count {
			get { return _source.Count; }
		}

		public ICollection<TKey> Keys {
			get { return _source.Keys; }
		}

		public ICollection<TValue> Values {
			get { return _source.Values; }
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly {
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether access to the dictionary
		/// is synchronized (thread safe).
		/// </summary>
		bool ICollection.IsSynchronized {
			get { return false; }
		}

		/// <summary>
		/// Gets an object that can be used to synchronize access to dictionary.
		/// </summary>
		object ICollection.SyncRoot {
			get {
				if (_syncRoot == null) {
					var collection = _source as ICollection;

					if (collection != null) {
						_syncRoot = collection.SyncRoot;
					} else {
						Interlocked.CompareExchange(ref _syncRoot, new object(), null);
					}
				}

				return _syncRoot;
			}
		}

		/// <summary>
		/// Gets or sets the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key of the value to get or set.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// Thrown when the key is null.
		/// </exception>
		/// <exception cref="T:System.Collections.Generic.KeyNotFoundException">
		/// The property is retrieved and key does not exist in the collection.
		/// </exception>
		public TValue this[TKey key] {
			get { return _source[key]; }
			set { ThrowNotSupportedException(); }
		}

		/// <summary>
		/// This method is not supported
		/// </summary>
		void IDictionary<TKey, TValue>.Add(TKey key, TValue value) {
			ThrowNotSupportedException();
		}

		public bool ContainsKey(TKey key) {
			return _source.ContainsKey(key);
		}

		/// <summary>
		/// This method is not supported
		/// </summary>
		bool IDictionary<TKey, TValue>.Remove(TKey key) {
			ThrowNotSupportedException();
			return false;
		}

		/// <summary>
		/// Gets the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="value">When this method returns, contains the value
		/// associated with the specified key, if the key is found;
		/// otherwise, the default value for the type of the value parameter.
		/// This parameter is passed uninitialized.</param>
		/// <returns>
		/// <b>true</b> if the dictionary contains
		/// an element with the specified key; otherwise, <b>false</b>.
		/// </returns>
		public bool TryGetValue(TKey key, out TValue value) {
			return _source.TryGetValue(key, out value);
		}

		/// <summary>
		/// This method is not supported
		/// </summary>
		void ICollection<KeyValuePair<TKey, TValue>>.Add(
			KeyValuePair<TKey, TValue> item) {
			ThrowNotSupportedException();
		}

		/// <summary>
		/// This method is not supported by the 
		/// </summary>
		void ICollection<KeyValuePair<TKey, TValue>>.Clear() {
			ThrowNotSupportedException();
		}

		/// <summary>
		/// Determines whether the dictionary contains a
		/// specific value.
		/// </summary>
		/// <returns>
		/// <b>true</b> if item is found in the <b>ICollection</b>; 
		/// otherwise, <b>false</b>.
		/// </returns>
		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(
			KeyValuePair<TKey, TValue> item) {
			ICollection<KeyValuePair<TKey, TValue>> collection = _source;

			return collection.Contains(item);
		}

		/// <summary>
		/// Copies the elements of the ICollection to an Array, starting at a
		/// particular Array index. 
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the
		/// destination of the elements copied from ICollection.
		/// The Array must have zero-based indexing.
		/// </param>
		/// <param name="arrayIndex">
		/// The zero-based index in array at which copying begins.
		/// </param>
		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(
			KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
			ICollection<KeyValuePair<TKey, TValue>> collection = _source;
			collection.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// This method is not supported
		/// </summary>
		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) {
			ThrowNotSupportedException();
			return false;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A IEnumerator that can be used to iterate through the collection.
		/// </returns>
		IEnumerator<KeyValuePair<TKey, TValue>>
			IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() {
			IEnumerable<KeyValuePair<TKey, TValue>> enumerator = _source;

			return enumerator.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An IEnumerator that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return _source.GetEnumerator();
		}

		/// <summary>
		/// For a description of this member, see <see cref="ICollection.CopyTo"/>. 
		/// </summary>
		/// <param name="array">
		/// The one-dimensional Array that is the destination of the elements copied from 
		/// ICollection. The Array must have zero-based indexing.
		/// </param>
		/// <param name="index">
		/// The zero-based index in Array at which copying begins.
		/// </param>
		void ICollection.CopyTo(Array array, int index) {
			ICollection collection =
				new List<KeyValuePair<TKey, TValue>>(_source);

			collection.CopyTo(array, index);
		}

		static void ThrowNotSupportedException() {
			throw new NotSupportedException("This Dictionary is read-only");

		}
	}

	internal sealed class ReadOnlyDictionaryDebugView<TKey, TValue> {
		IDictionary<TKey, TValue> dict;

		public ReadOnlyDictionaryDebugView(
			ReadOnlyDictionary<TKey, TValue> dictionary) {
			if (dictionary == null) {
				throw new ArgumentNullException("dictionary");
			}

			dict = dictionary;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public KeyValuePair<TKey, TValue>[] Items {
			get {
				var array = new KeyValuePair<TKey, TValue>[dict.Count];
				dict.CopyTo(array, 0);
				return array;
			}
		}
	}
}
