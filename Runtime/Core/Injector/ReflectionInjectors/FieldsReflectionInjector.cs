using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DependencyInjector.Core
{
    public class FieldsReflectionInjector : IReflectionInjector
    {
        private readonly Dictionary<Type, List<FieldInfo>> _cachedFields = new();
        private IDIContainer[] _diContainers;

        public Action<string> OnErrorThrown { get; set; }

        public void Inject(IDIContainer[] diContainers, object objectToSetInjections)
        {
            _diContainers = diContainers;
            List<FieldInfo> fields = GetFields(objectToSetInjections);

            SetFields(objectToSetInjections, fields);
        }

        private List<FieldInfo> GetFields(object objectToSetInjections)
        {
            Type objectType = objectToSetInjections.GetType();

            if (_cachedFields.TryGetValue(objectType, out List<FieldInfo> cached))
                return cached;

            List<FieldInfo> injectableFields = new List<FieldInfo>();
            Type currentType = objectType;
            while (true)
            {
                FieldInfo[] fieldInfos = currentType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (FieldInfo field in fieldInfos)
                {
                    if (field.GetCustomAttribute<InjectAttribute>() != null)
                        injectableFields.Add(field);
                }

                currentType = currentType.BaseType;

                if (ReferenceEquals(currentType, null) || currentType == typeof(MonoBehaviour))
                    break;
            }

            _cachedFields[objectType] = injectableFields;
            return injectableFields;
        }

        private void SetFields(object objectToSetInjections, List<FieldInfo> fields)
        {
            foreach (var fieldInfo in fields)
                SetFieldValue(objectToSetInjections, fieldInfo);
        }

        private void SetFieldValue(object objectToSetInjections, FieldInfo fieldInfo)
        {
            Type type = fieldInfo.FieldType;

            if (type.IsArray)
            {
                SetArrayField(objectToSetInjections, fieldInfo, type);
                return;
            }

            object value = GetFieldByElementType(type);
            fieldInfo.SetValue(objectToSetInjections, value);
        }

        private void SetArrayField(object objectToSetInjections, FieldInfo fieldInfo, Type type)
        {
            Type elementType = type.GetElementType();
            object[] fieldValue = GetFieldsByElementType(elementType);

            Array destinationArray = Array.CreateInstance(elementType, fieldValue.Length);
            Array.Copy(fieldValue, destinationArray, fieldValue.Length);

            fieldInfo.SetValue(objectToSetInjections, destinationArray);
        }

        private object[] GetFieldsByElementType(Type elementType)
        {
            foreach (var diContainer in _diContainers)
            {
                if (diContainer.IsTypeContained(elementType))
                    return diContainer.GetArrayByType(elementType);
            }
            
            string error = "FieldsReflectionInjector Error: GetCachedArrayByType can't return because it doesn't exist: " + elementType;
            OnErrorThrown?.Invoke(error);
            
            throw new Exception(error);
        }
        
        private object GetFieldByElementType(Type elementType)
        {
            foreach (var diContainer in _diContainers)
            {
                if(null == diContainer)
                    throw new Exception("DiContainer is null");
                
                if (diContainer.IsTypeContained(elementType))
                    return diContainer.GetObjectByType(elementType);
            }

            string error = "FieldsReflectionInjector Error: GetFieldByElementType can't return because it doesn't exist: " + elementType;
            OnErrorThrown?.Invoke(error);
            
            throw new Exception(error);
        }
    }
}