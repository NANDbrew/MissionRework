
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MissionRework
{
    // "borrowed" from SailwindModdingHelper by App24
    public static class Extensions
    {
        public static object GetPrivateField(this object obj, string field)
        {
            return Traverse.Create(obj).Field(field).GetValue();
        }

        public static T GetPrivateField<T>(this object obj, string field)
        {
            return (T)obj.GetPrivateField(field);
        }

        public static object GetPrivateField<T>(string field)
        {
            return Traverse.Create(typeof(T)).Field(field).GetValue();
        }

        public static T GetPrivateField<T, E>(string field)
        {
            return (T)GetPrivateField<E>(field);
        }

        public static void SetPrivateField(this object obj, string field, object value)
        {
            Traverse.Create(obj).Field(field).SetValue(value);
        }

        public static void SetPrivateField<T>(string field, object value)
        {
            Traverse.Create(typeof(T)).Field(field).SetValue(value);
        }

        public static object InvokePrivateMethod(this object obj, string method, params object[] parameters)
        {
            return AccessTools.Method(obj.GetType(), method).Invoke(obj, parameters);
        }

        public static T InvokePrivateMethod<T>(this object obj, string method, params object[] parameters)
        {
            return (T)obj.InvokePrivateMethod(method, parameters);
        }

        public static object InvokePrivateMethod<T>(string method, params object[] parameters)
        {
            return AccessTools.Method(typeof(T), method).Invoke(null, parameters);
        }

        public static T InvokePrivateMethod<T, E>(string method, params object[] parameters)
        {
            return (T)InvokePrivateMethod<E>(method, parameters);
        }

        public static T GetCopyOf<T>(this Component comp, T other) where T : Component
        {
            Type type = comp.GetType();
            if (type != other.GetType())
            {
                return null;
            }

            BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo[] properties = type.GetProperties(bindingAttr);
            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.CanWrite)
                {
                    try
                    {
                        propertyInfo.SetValue(comp, propertyInfo.GetValue(other, null), null);
                    }
                    catch
                    {
                    }
                }
            }

            FieldInfo[] fields = type.GetFields(bindingAttr);
            foreach (FieldInfo fieldInfo in fields)
            {
                fieldInfo.SetValue(comp, fieldInfo.GetValue(other));
            }

            return comp as T;
        }
    /*
        public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
        {
            return go.AddComponent<T>().GetCopyOf(toAdd);
        }*/

        public static Transform GetChildByName(this Transform transform, string childName)
        {
            foreach (Transform item in transform)
            {
                if (item.name == childName)
                {
                    return item;
                }
            }

            return null;
        }
    }
}