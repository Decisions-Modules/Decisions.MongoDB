using DecisionsFramework.Design.Flow;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Decisions.MongoDB
{
    internal class FetchStepUtility
    {
        internal static FilterDefinition<TDocument> GetCombinedFilter<TDocument>(MongoDBFilter[] filters, StepStartData data, bool combineUsingAnd = true)
        {
            if (filters == null || filters.Length == 0)
                return null;
            FilterDefinition<TDocument> result = null;
            foreach (MongoDBFilter filter in filters)
            {
                FilterDefinition<TDocument> mongoFilter = GetFilter<TDocument>(filter, data);
                if (mongoFilter == null)
                    continue;

                if (result == null)
                    result = mongoFilter;
                else
                {
                    if (combineUsingAnd)
                        result = result & mongoFilter;
                    else
                        result = result | mongoFilter;
                }
            }
            return result;
        }

        internal static FilterDefinition<TDocument> GetFilter<TDocument>(MongoDBFilter filter, StepStartData data)
        {
            if (filter.FilterType == MongoFilterType.FieldName)
            {
                object value = data[filter.GetInputName()];
                PropertyInfo prop = typeof(TDocument).GetProperty(filter.FieldName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                    throw new Exception($"Property '{filter.FieldName}' not found on type '{typeof(TDocument).FullName}'");

                MethodInfo mi = typeof(FetchStepUtility).GetMethod(nameof(GetFilterFromMatchType), BindingFlags.NonPublic | BindingFlags.Static);
                if (mi == null)
                    throw new Exception("Error finding filter method");

                mi = mi.MakeGenericMethod(typeof(TDocument), prop.PropertyType);
                object result = mi.Invoke(null, new object[] { filter.MatchType, filter.FieldName, value });
                return result as FilterDefinition<TDocument>;
            }
            else if (filter.FilterType == MongoFilterType.NestedDocumentFieldName)
            {
                object value = data[filter.GetInputName()];
                switch (filter.FieldType)
                {
                    case MongoFieldType.Boolean: return GetFilterFromMatchType<TDocument, bool?>(filter.MatchType, filter.FieldPath, value);
                    case MongoFieldType.DateTime: return GetFilterFromMatchType<TDocument, DateTime?>(filter.MatchType, filter.FieldPath, value);
                    case MongoFieldType.Decimal: return GetFilterFromMatchType<TDocument, decimal?>(filter.MatchType, filter.FieldPath, value);
                    case MongoFieldType.Integer: return GetFilterFromMatchType<TDocument, int?>(filter.MatchType, filter.FieldPath, value);
                    case MongoFieldType.String: return GetFilterFromMatchType<TDocument, string>(filter.MatchType, filter.FieldPath, value);
                    default: throw new Exception("Invalid MongoFieldType");
                }
            }
            else if (filter.FilterType == MongoFilterType.CombineAnd)
            {
                return GetCombinedFilter<TDocument>(filter.SubFilters, data, true);
            }
            else if (filter.FilterType == MongoFilterType.CombineOr)
            {
                return GetCombinedFilter<TDocument>(filter.SubFilters, data, false);
            }
            return null;
        }

        private static FilterDefinition<TDocument> GetFilterFromMatchType<TDocument, TField>(MongoQueryMatchType matchType, string fieldPath, object value)
        {
            FieldDefinition<TDocument, TField> fieldDef = (FieldDefinition<TDocument, TField>)fieldPath;

            switch (matchType)
            {
                case MongoQueryMatchType.Equals:
                    return Builders<TDocument>.Filter.Eq(fieldDef, (TField)value);
                case MongoQueryMatchType.DoesNotEqual:
                    return Builders<TDocument>.Filter.Ne(fieldDef, (TField)value);
                case MongoQueryMatchType.GreaterThanOrEqualTo:
                    return Builders<TDocument>.Filter.Gte(fieldDef, (TField)value);
                case MongoQueryMatchType.LessThanOrEqualTo:
                    return Builders<TDocument>.Filter.Lte(fieldDef, (TField)value);
                case MongoQueryMatchType.GreaterThan:
                    return Builders<TDocument>.Filter.Gt(fieldDef, (TField)value);
                case MongoQueryMatchType.LessThan:
                    return Builders<TDocument>.Filter.Lt(fieldDef, (TField)value);
                case MongoQueryMatchType.Exists:
                    return Builders<TDocument>.Filter.Exists(fieldDef, true);
                case MongoQueryMatchType.DoesNotExist:
                    return Builders<TDocument>.Filter.Exists(fieldDef, false);
                default: throw new Exception("Invalid MongoQueryMatchType");
            }
        }

        internal static Type GetTypeFromMongoFieldType(MongoFieldType fieldType)
        {
            switch (fieldType)
            {
                case MongoFieldType.Boolean: return typeof(bool?);
                case MongoFieldType.DateTime: return typeof(DateTime?);
                case MongoFieldType.Decimal: return typeof(decimal?);
                case MongoFieldType.Integer: return typeof(int?);
                case MongoFieldType.String: return typeof(string);
                default: throw new ArgumentException("Invalid MongoFieldType");
            }
        }

        internal static FilterDefinition<TDocument> GetIdMatchFilter<TDocument>(object id, IdType idType)
        {
            switch (idType)
            {
                case IdType.StringOrObjectId:
                    string value = id as string;
                    ObjectId objectId;
                    if (ObjectId.TryParse(value, out objectId))
                    {
                        return Builders<TDocument>.Filter.Eq("_id", objectId);
                    }
                    else
                    {
                        return Builders<TDocument>.Filter.Eq("_id", value);
                    }
                case IdType.Int32:
                    return Builders<TDocument>.Filter.Eq("_id", (int)id);
                case IdType.Int64:
                    return Builders<TDocument>.Filter.Eq("_id", (long)id);
                case IdType.Float:
                    return Builders<TDocument>.Filter.Eq("_id", (float)id);
                case IdType.Double:
                    return Builders<TDocument>.Filter.Eq("_id", (double)id);
                default:
                    throw new ArgumentException("Unknown IdType " + idType.ToString());
            }
        }

        internal static FilterDefinition<TDocument> GetIdsInFilter<TDocument>(IEnumerable<object> ids, IdType idType)
        {
            switch (idType)
            {
                case IdType.StringOrObjectId:
                    return Builders<TDocument>.Filter.In("_id", ids
                        .Select(id => ObjectId.TryParse(id as string, out ObjectId oid) ? oid : id).ToList());
                case IdType.Int32:
                    return Builders<TDocument>.Filter.In("_id", ids.Select(id => (int)id).ToList());
                case IdType.Int64:
                    return Builders<TDocument>.Filter.In("_id", ids.Select(id => (long)id).ToList()); 
                case IdType.Float:
                    return Builders<TDocument>.Filter.In("_id", ids.Select(id => (float)id).ToList());  
                case IdType.Double:
                    return Builders<TDocument>.Filter.In("_id", ids.Select(id => (double)id).ToList());  
                default:
                    throw new ArgumentException("Unknown IdType " + idType);
            }
        }

        internal static SortDefinition<TDocument> GetSortDefinition<TDocument>(MongoDBSort[] sortFields)
        {
            SortDefinition<TDocument> sortDef = null;
            foreach (MongoDBSort sortField in sortFields)
            {
                if (sortField.SortOrder == MongoDBSortOrder.Ascending)
                {
                    if (sortDef == null)
                    {
                        sortDef = Builders<TDocument>.Sort.Ascending(sortField.FieldName);
                    }
                    else
                    {
                        sortDef = sortDef.Ascending(sortField.FieldName);
                    }
                }
                else
                {
                    if (sortDef == null)
                    {
                        sortDef = Builders<TDocument>.Sort.Descending(sortField.FieldName);
                    }
                    else
                    {
                        sortDef = sortDef.Descending(sortField.FieldName);
                    }
                }
            }
            return sortDef;
        }
    }
}
