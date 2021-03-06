﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PingSandbox
{
	public static class ExtIEnumerable
	{

		public static List<List<T>> Chunk<T>(this IEnumerable<T> collection, int chunkSize)
		{
			List<List<T>> collections = new List<List<T>>();

			int nrCollections = Convert.ToInt32(Math.Ceiling(collection.Count() / (double)chunkSize));

			for (int i = 0; i < nrCollections; i++)
			{
				List<T> _collection = collection.Skip(i * chunkSize).Take(chunkSize).ToList();
				collections.Add(_collection);
			}

			return collections;
		}

	}
}
