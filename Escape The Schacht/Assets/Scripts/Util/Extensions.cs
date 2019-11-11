using System;
using System.Collections.Generic;
using System.Text;

public static class Extensions {

    public static readonly Random r = new Random();

    public static void AddAll<T>(this ICollection<T> collection, params T[] items) {
        foreach (T item in items)
            collection.Add(item);
    }

    public static void Shuffle<T>(this T[] array) {
        if (array.Length < 2)
            return;

        for (int i = 0; i < array.Length - 1; i++) {

            int index = r.Next(i, array.Length);
            T temp = array[i];
            array[i] = array[index];
            array[index] = temp;
        }
    }

    public static int Count<T>(this T[] array, T elem) {
        int result = 0;
        foreach (T e in array)
            if (elem.Equals(e))
                result++;
        return result;
    }

    public static float Sum(this float[] array) {
        float result = 0;
        foreach (float f in array)
            result += f;
        return result;
    }

    public static int Sum(this int[] array) {
        int result = 0;
        foreach (int i in array)
            result += i;
        return result;
    }


    public static bool Contains<T>(this T[] array, T elem) {
        foreach (T e in array) {
            if (elem == null) {
                if (e == null)
                    return true;
            }
            else if (elem.Equals(e))
                return true;
        }
        return false;
    }

    public static string ToString1<T>(this IEnumerable<T> enumerable) {
        StringBuilder sb = new StringBuilder();
        sb.Append('[');
        bool empty = true;
        foreach (T item in enumerable) {
            if (!empty)
                sb.Append(", ");
            sb.Append(item.ToString());
            empty = false;
        }
        sb.Append(']');
        return sb.ToString();
    }

    public static T[] ToArray<T>(this ICollection<T> collection) {
        T[] result = new T[collection.Count];
        int i = 0;
        foreach (T item in collection)
            result[i++] = item;
        return result;
    }

}
