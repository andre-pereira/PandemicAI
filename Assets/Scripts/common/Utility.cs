using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;
using Newtonsoft.Json;
using System.Linq;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Provides utility extension methods for various Unity and collection operations.
    /// </summary>
    public static class Utility
    {
        #region Execute Later

        /// <summary>
        /// Executes a function after a specified delay.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour used to start the coroutine.</param>
        /// <param name="delay">Delay in seconds before executing the function.</param>
        /// <param name="fn">The function to execute.</param>
        public static void ExecuteLater(this MonoBehaviour behaviour, float delay, Action fn)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));

            if (fn == null)
                throw new ArgumentNullException(nameof(fn));

            behaviour.StartCoroutine(RealExecute(delay, fn));
        }

        /// <summary>
        /// Coroutine that waits for a delay before executing the given function.
        /// </summary>
        /// <param name="delay">Delay in seconds.</param>
        /// <param name="fn">The function to execute after the delay.</param>
        /// <returns>An IEnumerator for coroutine handling.</returns>
        private static IEnumerator RealExecute(float delay, Action fn)
        {
            yield return new WaitForSeconds(delay);
            fn();
        }

        #endregion

        #region Pop from List

        /// <summary>
        /// Removes and returns the element at the specified index from the list.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <param name="list">The list from which to remove the element.</param>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <returns>The removed element.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
        public static T Pop<T>(this IList<T> list, int index)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (index < 0 || index >= list.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

            T item = list[index];
            list.RemoveAt(index);
            return item;
        }

        /// <summary>
        /// Removes and returns the last element from the list.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <param name="list">The list from which to remove the last element.</param>
        /// <returns>The removed last element.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the list is null or empty.</exception>
        public static T Pop<T>(this List<T> list)
        {
            if (list == null || list.Count == 0)
                throw new InvalidOperationException("Cannot pop from an empty list.");

            int lastIndex = list.Count - 1;
            T item = list[lastIndex];
            list.RemoveAt(lastIndex);
            return item;
        }

        #endregion

        #region Destroy Children

        /// <summary>
        /// Immediately destroys all children of the specified GameObject.
        /// </summary>
        /// <param name="go">The GameObject whose children will be destroyed.</param>
        public static void DestroyChildrenImmediate(this GameObject go)
        {
            if (go == null)
                throw new ArgumentNullException(nameof(go));

            // Collect all children into an array to avoid modifying the collection during iteration.
            int childCount = go.transform.childCount;
            GameObject[] children = new GameObject[childCount];

            for (int i = 0; i < childCount; ++i)
            {
                children[i] = go.transform.GetChild(i).gameObject;
            }

            foreach (GameObject child in children)
            {
                GameObject.DestroyImmediate(child);
            }
        }

        #endregion

        #region Shuffle

        /// <summary>
        /// Shuffles the elements of the list in-place using the specified random state.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        /// <param name="randomState">The random state to use for shuffling.</param>
        public static void Shuffle<T>(this IList<T> list, Random.State randomState)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            Random.state = randomState;
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                // Swap the elements at indices k and n.
                T temp = list[k];
                list[k] = list[n];
                list[n] = temp;
            }
        }

        #endregion

        #region JSONHandling

        // Flattens nested dictionaries and arrays into dot/bracket notation keys
        private static void FlattenObject(object obj, Dictionary<string, object> result, string prefix = "")
        {
            if (obj is IDictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    string key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
                    FlattenObject(kvp.Value, result, key);
                }
            }
            else if (obj is Newtonsoft.Json.Linq.JObject jObj)
            {
                foreach (var prop in jObj.Properties())
                {
                    string key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    FlattenObject(prop.Value, result, key);
                }
            }
            else if (obj is Newtonsoft.Json.Linq.JArray jArr)
            {
                for (int i = 0; i < jArr.Count; i++)
                {
                    string key = $"{prefix}[{i}]";
                    FlattenObject(jArr[i], result, key);
                }
            }
            else if (obj is IList<object> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    string key = $"{prefix}[{i}]";
                    FlattenObject(list[i], result, key);
                }
            }
            else if (obj is Newtonsoft.Json.Linq.JValue jVal)
            {
                result[prefix] = jVal.Value;
            }
            else
            {
                result[prefix] = obj;
            }
        }

        public static Dictionary<string, object> ParseEventText(string jsonString)
        {
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
                if (dict == null)
                    return new Dictionary<string, object>();

                // Flatten nested objects/arrays if needed
                var flat = new Dictionary<string, object>();
                FlattenObject(dict, flat);

                // Convert city numbers to names
                ConvertCityNumbersToNames(flat);

                return flat;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing event JSON: {e.Message}");
                return new Dictionary<string, object>();
            }
        }

        // Converts city number fields to city names, works with object values
        private static void ConvertCityNumbersToNames(Dictionary<string, object> variables)
        {
            foreach (var kvp in variables.ToList())
            { // remove cityCard and create information such as city colour and so on
                if (kvp.Key == "city" || kvp.Key == "cityCard" || kvp.Key == "fromCity" || kvp.Key == "toCity" ||
                    kvp.Key.StartsWith("infectCities["))
                {
                    try
                    {
                        int cityNumber = Convert.ToInt32(kvp.Value);
                        if (cityNumber == 24)
                        {
                            variables[kvp.Key] = "Epidemic";
                        }
                        else
                            variables[kvp.Key] = CityDrawer.CityScripts[cityNumber].name;
                    }
                    catch
                    {
                        Debug.Log($"Error converting city number to city name for key: {kvp.Key}, value: {kvp.Value}");
                    }
                }
                else
                    variables[kvp.Key] = kvp.Value;
            }
        }

        public static Dictionary<string, string> convertVariablesToString(Dictionary<string, object> variables)
        {
            Dictionary<string, string> variablesString = new Dictionary<string, string>();
            foreach (var kvp in variables)
            {
                if (kvp.Value is Enums.VirusName)
                    variablesString[kvp.Key] = ((Enums.VirusName)kvp.Value).ToString();
                variablesString[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
            }

            return variablesString;
        }

        #endregion

    }
}
