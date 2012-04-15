namespace System.Data.Entity.Core.EntityClient
{
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Common.Internal;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Diagnostics;

    /// <summary>
    /// Class representing a parameter used in EntityCommand
    /// </summary>
    public sealed partial class EntityParameter : DbParameter, IDbDataParameter
    {
        private string _parameterName;
        private DbType? _dbType;
        private EdmType _edmType;
        private byte? _precision;
        private byte? _scale;
        private bool _isDirty;

        /// <summary>
        /// Constructs the EntityParameter object
        /// </summary>
        public EntityParameter()
        {
        }

        /// <summary>
        /// Constructs the EntityParameter object with the given parameter name and the type of the parameter
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="dbType">The type of the parameter</param>
        public EntityParameter(string parameterName, DbType dbType)
        {
            SetParameterNameWithValidation(parameterName, "parameterName");
            DbType = dbType;
        }

        /// <summary>
        /// Constructs the EntityParameter object with the given parameter name, the type of the parameter, and the size of the
        /// parameter
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="dbType">The type of the parameter</param>
        /// <param name="size">The size of the parameter</param>
        public EntityParameter(string parameterName, DbType dbType, int size)
        {
            SetParameterNameWithValidation(parameterName, "parameterName");
            DbType = dbType;
            Size = size;
        }

        /// <summary>
        /// Constructs the EntityParameter object with the given parameter name, the type of the parameter, the size of the
        /// parameter, and the name of the source column
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="dbType">The type of the parameter</param>
        /// <param name="size">The size of the parameter</param>
        /// <param name="sourceColumn">The name of the source column mapped to the data set, used for loading the parameter value</param>
        public EntityParameter(string parameterName, DbType dbType, int size, string sourceColumn)
        {
            SetParameterNameWithValidation(parameterName, "parameterName");
            DbType = dbType;
            Size = size;
            SourceColumn = sourceColumn;
        }

        /// <summary>
        /// Constructs the EntityParameter object with the given parameter name, the type of the parameter, the size of the
        /// parameter, and the name of the source column
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="dbType">The type of the parameter</param>
        /// <param name="size">The size of the parameter</param>
        /// <param name="direction">The direction of the parameter, whether it's input/output/both/return value</param>
        /// <param name="isNullable">If the parameter is nullable</param>
        /// <param name="precision">The floating point precision of the parameter, valid only if the parameter type is a floating point type</param>
        /// <param name="scale">The scale of the parameter, valid only if the parameter type is a floating point type</param>
        /// <param name="sourceColumn">The name of the source column mapped to the data set, used for loading the parameter value</param>
        /// <param name="sourceVersion">The data row version to use when loading the parameter value</param>
        /// <param name="value">The value of the parameter</param>
        public EntityParameter(
            string parameterName,
            DbType dbType,
            int size,
            ParameterDirection direction,
            bool isNullable,
            byte precision,
            byte scale,
            string sourceColumn,
            DataRowVersion sourceVersion,
            object value)
        {
            SetParameterNameWithValidation(parameterName, "parameterName");
            DbType = dbType;
            Size = size;
            Direction = direction;
            IsNullable = isNullable;
            Precision = precision;
            Scale = scale;
            SourceColumn = sourceColumn;
            SourceVersion = sourceVersion;
            Value = value;
        }

        /// <summary>
        /// The name of the parameter
        /// </summary>
        public override string ParameterName
        {
            get { return _parameterName ?? ""; }
            set { SetParameterNameWithValidation(value, "value"); }
        }

        /// <summary>
        /// Helper method to validate the parameter name; Ideally we'd only call this once, but 
        /// we have to put an argumentName on the Argument exception, and the property setter would
        /// need "value" which confuses folks when they call the constructor that takes the value 
        /// of the parameter.  c'est la vie.
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="argumentName"></param>
        private void SetParameterNameWithValidation(string parameterName, string argumentName)
        {
            if (!string.IsNullOrEmpty(parameterName)
                && !DbCommandTree.IsValidParameterName(parameterName))
            {
                throw EntityUtil.Argument(Strings.EntityClient_InvalidParameterName(parameterName), argumentName);
            }
            PropertyChanging();
            _parameterName = parameterName;
        }

        /// <summary>
        /// The type of the parameter, EdmType may also be set, and may provide more detailed information.
        /// </summary>
        public override DbType DbType
        {
            get
            {
                // if the user has not set the dbType but has set the dbType, use the edmType to try to deduce a dbType
                if (!_dbType.HasValue)
                {
                    if (_edmType != null)
                    {
                        return GetDbTypeFromEdm(_edmType);
                    }
                        // If the user has set neither the DbType nor the EdmType, 
                        // then we attempt to deduce it from the value, but we won't set it in the
                        // member field as that's used to keep track of what the user set explicitly
                    else
                    {
                        // If we can't deduce the type because there are no values, we still have to return something, just assume it's string type
                        if (_value == null)
                        {
                            return DbType.String;
                        }

                        try
                        {
                            return TypeHelpers.ConvertClrTypeToDbType(_value.GetType());
                        }
                        catch (ArgumentException e)
                        {
                            throw EntityUtil.InvalidOperation(Strings.EntityClient_CannotDeduceDbType, e);
                        }
                    }
                }

                return (DbType)_dbType;
            }
            set
            {
                PropertyChanging();
                _dbType = value;
            }
        }

        /// <summary>
        /// The type of the parameter, expressed as an EdmType.
        /// May be null (which is what it will be if unset).  This means
        /// that the DbType contains all the type information.
        /// Non-null values must not contradict DbType (only restate or specialize).
        /// </summary>
        public EdmType EdmType
        {
            get { return _edmType; }
            set
            {
                if (value != null
                    && !Helper.IsScalarType(value))
                {
                    throw EntityUtil.InvalidOperation(Strings.EntityClient_EntityParameterEdmTypeNotScalar(value.FullName));
                }
                PropertyChanging();
                _edmType = value;
            }
        }

        /// <summary>
        /// The precision of the parameter if the parameter is a floating point type
        /// </summary>
        public byte Precision
        {
            get
            {
                var result = _precision.HasValue ? _precision.Value : (byte)0;
                return result;
            }
            set
            {
                PropertyChanging();
                _precision = value;
            }
        }

        /// <summary>
        /// The scale of the parameter if the parameter is a floating point type
        /// </summary>
        public byte Scale
        {
            get
            {
                var result = _scale.HasValue ? _scale.Value : (byte)0;
                return result;
            }
            set
            {
                PropertyChanging();
                _scale = value;
            }
        }

        /// <summary>
        /// The value of the parameter
        /// </summary>
        public override object Value
        {
            get { return _value; }
            set
            {
                // If the user hasn't set the DbType, then we have to figure out if the DbType will change as a result
                // of the change in the value.  What we want to achieve is that changes to the value will not cause
                // it to be dirty, but changes to the value that causes the apparent DbType to change, then should be
                // dirty.
                if (!_dbType.HasValue
                    && _edmType == null)
                {
                    // If the value is null, then we assume it's string type
                    var oldDbType = DbType.String;
                    if (_value != null)
                    {
                        oldDbType = TypeHelpers.ConvertClrTypeToDbType(_value.GetType());
                    }

                    // If the value is null, then we assume it's string type
                    var newDbType = DbType.String;
                    if (value != null)
                    {
                        newDbType = TypeHelpers.ConvertClrTypeToDbType(value.GetType());
                    }

                    if (oldDbType != newDbType)
                    {
                        PropertyChanging();
                    }
                }

                _value = value;
            }
        }

        /// <summary>
        /// Gets whether this collection has been changes since the last reset
        /// </summary>
        internal bool IsDirty
        {
            get { return _isDirty; }
        }

        /// <summary>
        /// Indicates whether the DbType property has been set by the user;
        /// </summary>
        internal bool IsDbTypeSpecified
        {
            get { return _dbType.HasValue; }
        }

        /// <summary>
        /// Indicates whether the Direction property has been set by the user;
        /// </summary>
        internal bool IsDirectionSpecified
        {
            get { return _direction != 0; }
        }

        /// <summary>
        /// Indicates whether the IsNullable property has been set by the user;
        /// </summary>
        internal bool IsIsNullableSpecified
        {
            get { return _isNullable.HasValue; }
        }

        /// <summary>
        /// Indicates whether the Precision property has been set by the user;
        /// </summary>
        internal bool IsPrecisionSpecified
        {
            get { return _precision.HasValue; }
        }

        /// <summary>
        /// Indicates whether the Scale property has been set by the user;
        /// </summary>
        internal bool IsScaleSpecified
        {
            get { return _scale.HasValue; }
        }

        /// <summary>
        /// Indicates whether the Size property has been set by the user;
        /// </summary>
        internal bool IsSizeSpecified
        {
            get { return _size.HasValue; }
        }

        /// <summary>
        /// Resets the DbType property to its original settings
        /// </summary>
        public override void ResetDbType()
        {
            if (_dbType != null
                || _edmType != null)
            {
                PropertyChanging();
            }

            _edmType = null;
            _dbType = null;
        }

        /// <summary>
        /// Clones this parameter object
        /// </summary>
        /// <returns>The new cloned object</returns>
        internal EntityParameter Clone()
        {
            return new EntityParameter(this);
        }

        /// <summary>
        /// Clones this parameter object
        /// </summary>
        /// <returns>The new cloned object</returns>
        private void CloneHelper(EntityParameter destination)
        {
            CloneHelperCore(destination);

            destination._parameterName = _parameterName;
            destination._dbType = _dbType;
            destination._edmType = _edmType;
            destination._precision = _precision;
            destination._scale = _scale;
        }

        /// <summary>
        /// Marks that this parameter has been changed
        /// </summary>
        private void PropertyChanging()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Determines the size of the given object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int ValueSize(object value)
        {
            return ValueSizeCore(value);
        }

        /// <summary>
        /// Get the type usage for this parameter in model terms.
        /// </summary>
        /// <returns>The type usage for this parameter</returns>
        //NOTE: Because GetTypeUsage throws CommandValidationExceptions, it should only be called from EntityCommand during command execution
        internal TypeUsage GetTypeUsage()
        {
            TypeUsage typeUsage;
            if (!IsTypeConsistent)
            {
                throw EntityUtil.InvalidOperation(
                    Strings.EntityClient_EntityParameterInconsistentEdmType(
                        _edmType.FullName, _parameterName));
            }

            if (_edmType != null)
            {
                typeUsage = TypeUsage.Create(_edmType);
            }
            else if (!DbTypeMap.TryGetModelTypeUsage(DbType, out typeUsage))
            {
                // Spatial types have only DbType 'Object', and cannot be represented in the static type map.
                PrimitiveType primitiveParameterType;
                if (DbType == DbType.Object &&
                    Value != null &&
                    ClrProviderManifest.Instance.TryGetPrimitiveType(Value.GetType(), out primitiveParameterType)
                    &&
                    Helper.IsSpatialType(primitiveParameterType))
                {
                    typeUsage = EdmProviderManifest.Instance.GetCanonicalModelTypeUsage(primitiveParameterType.PrimitiveTypeKind);
                }
                else
                {
                    throw EntityUtil.InvalidOperation(Strings.EntityClient_UnsupportedDbType(DbType.ToString(), ParameterName));
                }
            }

            Debug.Assert(typeUsage != null, "DbType.TryGetModelTypeUsage returned true for null TypeUsage?");
            return typeUsage;
        }

        /// <summary>
        /// Reset the dirty flag on the collection
        /// </summary>
        internal void ResetIsDirty()
        {
            _isDirty = false;
        }

        private bool IsTypeConsistent
        {
            get
            {
                if (_edmType != null
                    && _dbType.HasValue)
                {
                    var dbType = GetDbTypeFromEdm(_edmType);
                    if (dbType == DbType.String)
                    {
                        // would need facets to distinguish the various sorts of string, 
                        // a generic string EdmType is consistent with any string DbType.
                        return _dbType == DbType.String || _dbType == DbType.AnsiString
                               || dbType == DbType.AnsiStringFixedLength || dbType == DbType.StringFixedLength;
                    }
                    else
                    {
                        return _dbType == dbType;
                    }
                }

                return true;
            }
        }

        private static DbType GetDbTypeFromEdm(EdmType edmType)
        {
            var primitiveType = Helper.AsPrimitive(edmType);
            DbType dbType;
            if (Helper.IsSpatialType(primitiveType))
            {
                return DbType.Object;
            }
            else if (DbCommandDefinition.TryGetDbTypeFromPrimitiveType(primitiveType, out dbType))
            {
                return dbType;
            }

            // we shouldn't ever get here.   Assert in a debug build, and pick a type.
            Debug.Assert(false, "The provided edmType is of an unknown primitive type.");
            return default(DbType);
        }
    }
}