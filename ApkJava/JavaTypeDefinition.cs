using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public abstract class JavaTypeDefinition : BaseAnnotableObject, IJavaType
    {
        static JavaTypeDefinition()
        {
            InitializeTypeSystem();
        }

        #region CONSTRCUTORS
        protected JavaTypeDefinition(NamingspaceItem container, string name)
        {
            if (string.IsNullOrEmpty(name)) { throw new ArgumentNullException(); }
            if (null == container) { throw new ArgumentNullException(); }
            this.Name = name;
            this.Namespace = container;
            container.Register(this);
            return;
        }

        /// <summary></summary>
        /// <param name="indexed"></param>
        /// <remarks>This constructor is for <see cref="ArrayType"/> use only.</remarks>
        protected JavaTypeDefinition(JavaTypeDefinition indexed)
        {
            if (null == indexed) { throw new ArgumentNullException(); }
            NamingspaceItem.Root.Register(this);
            return;
        }

        protected JavaTypeDefinition(string name)
        {
            if (string.IsNullOrEmpty(name)) { throw new ArgumentNullException(); }
            // Parse name accordding to the grammar defined in 4.3.2 Field descriptors
            // from the reference document cited in JavaClassFileLiteParser
            StringBuilder builder = new StringBuilder(name);
            if ((0 < builder.Length) && ('[' == builder[0])) {
                throw new InvalidOperationException();
            }
            JavaTypeDefinition ultimate;
            if (0 == builder.Length) {
                throw new InvalidJavaTypeException();
            }
            switch (builder[0]) {
                case 'B':
                    ultimate = NamingspaceItem.Root["byte"];
                    builder.Remove(0, 1);
                    break;
                case 'C':
                    ultimate = NamingspaceItem.Root["char"];
                    builder.Remove(0, 1);
                    break;
                case 'D':
                    ultimate = NamingspaceItem.Root["double"];
                    builder.Remove(0, 1);
                    break;
                case 'F':
                    ultimate = NamingspaceItem.Root["float"];
                    builder.Remove(0, 1);
                    break;
                case 'I':
                    ultimate = NamingspaceItem.Root["int"];
                    builder.Remove(0, 1);
                    break;
                case 'J':
                    ultimate = NamingspaceItem.Root["long"];
                    builder.Remove(0, 1);
                    break;
                case 'S':
                    ultimate = NamingspaceItem.Root["short"];
                    builder.Remove(0, 1);
                    break;
                case 'V':
                    ultimate = NamingspaceItem.Root["void"];
                    builder.Remove(0, 1);
                    break;
                case 'Z':
                    ultimate = NamingspaceItem.Root["boolean"];
                    builder.Remove(0, 1);
                    break;
                case 'L':
                    builder.Remove(0, 1);
                    bool terminatorFound = false;
                    for(int index = 0; index < builder.Length; index++) {
                        if (';' == builder[index]) {
                            if ((builder.Length - 1) != index) {
                                throw new InvalidJavaTypeException();
                            }
                            terminatorFound = true;
                            builder.Remove(index, 1);
                            break;
                        }
                    }
                    if (0 == builder.Length) { throw new InvalidJavaTypeException(); }
                    if (!terminatorFound) { throw new InvalidJavaTypeException(); }
                    string[] nameItems = builder.ToString().Split('/');
                    builder.Clear();
                    NamingspaceItem currentNamespace = NamingspaceItem.Root;
                    for(int index = 0; index < (nameItems.Length - 1); index++) {
                        if (string.IsNullOrEmpty(nameItems[index])) {
                            throw new InvalidJavaTypeException();
                        }
                        currentNamespace = currentNamespace.GetOrCreateSon(nameItems[index]);
                        if (null == currentNamespace) {
                            throw new InvalidJavaTypeException();
                        }
                    }
                    string simpleName = nameItems[nameItems.Length - 1];
                    if (string.IsNullOrEmpty(simpleName)) {
                        throw new InvalidJavaTypeException();
                    }
                    this.Name = simpleName;
                    this.Namespace = currentNamespace;
                    currentNamespace.Register(this);
                    break;
                default:
                    throw new InvalidOperationException();
            }
            if (0 != builder.Length) {
                throw new InvalidJavaTypeException();
            }
            return;
        }
        #endregion

        #region PROPERTIES
        public string FullyQualifiedName
        {
            get
            {
                if (null == _fullyQualifiedName) {
                    StringBuilder builder = new StringBuilder();
                    builder.Append(Name);
                    for (NamingspaceItem item = Namespace;
                        !object.ReferenceEquals(NamingspaceItem.Root, item);
                        item = item.Parent)
                    {
                        builder.Insert(0, NamespaceItemSeparator);
                        builder.Insert(0, item.Name);
                    }
                    _fullyQualifiedName = builder.ToString();
                }
                return _fullyQualifiedName;
            }
            protected set
            {
                throw new InvalidOperationException();
            }
        }

        public virtual JavaTypeDefinition IndexedType
        {
            get { throw new InvalidOperationException(); }
        }

        public virtual bool IsArray
        {
            get { return false; }
        }

        public abstract bool IsBuiltin { get; }

        /// <summary>The simple name of the type, that is the name stripped from
        /// any namespace related item.</summary>
        public string Name { get; protected set; }

        public NamingspaceItem Namespace { get; protected set; }

        public IJavaType SuperType { get; protected set; }
        #endregion

        #region METHODS
        public static JavaTypeDefinition Get(string name)
        {
            JavaTypeDefinition result = TryGet(name);
            if (null != result) { return result; }
            throw new InvalidJavaTypeException();
        }

        public static JavaTypeDefinition TryGet(string name)
        {
            if (string.IsNullOrEmpty(name)) { throw new ArgumentNullException(); }
            // Parse name accordding to the grammar defined in 4.3.2 Field descriptors
            // from the reference document cited in JavaClassFileLiteParser
            StringBuilder builder = new StringBuilder(name);
            int indirectionCount = 0;
            while ((0 < builder.Length) && ('[' == builder[0])) {
                indirectionCount++;
                builder.Remove(0, 1);
            }
            JavaTypeDefinition result;
            if (0 == builder.Length) {
                throw new InvalidJavaTypeException();
            }
            switch (builder[0]) {
                case 'B':
                    builder.Remove(0, 1);
                    result = NamingspaceItem.Root["byte"];
                    break;
                case 'C':
                    result = NamingspaceItem.Root["char"];
                    builder.Remove(0, 1);
                    break;
                case 'D':
                    result = NamingspaceItem.Root["double"];
                    builder.Remove(0, 1);
                    break;
                case 'F':
                    result = NamingspaceItem.Root["float"];
                    builder.Remove(0, 1);
                    break;
                case 'I':
                    result = NamingspaceItem.Root["int"];
                    builder.Remove(0, 1);
                    break;
                case 'J':
                    result = NamingspaceItem.Root["long"];
                    builder.Remove(0, 1);
                    break;
                case 'S':
                    result = NamingspaceItem.Root["short"];
                    builder.Remove(0, 1);
                    break;
                case 'V':
                    result = NamingspaceItem.Root["void"];
                    builder.Remove(0, 1);
                    break;
                case 'Z':
                    result = NamingspaceItem.Root["boolean"];
                    builder.Remove(0, 1);
                    break;
                case 'L':
                    builder.Remove(0, 1);
                    bool terminatorFound = false;
                    for (int index = 0; index < builder.Length; index++) {
                        if (';' == builder[index]) {
                            if ((builder.Length - 1) != index) {
                                throw new InvalidJavaTypeException();
                            }
                            terminatorFound = true;
                            builder.Remove(index, 1);
                            break;
                        }
                    }
                    if (0 == builder.Length) { throw new InvalidJavaTypeException(); }
                    if (!terminatorFound) { throw new InvalidJavaTypeException(); }
                    string[] nameItems = builder.ToString().Split('/');
                    builder.Clear();
                    NamingspaceItem currentNamespace = NamingspaceItem.Root;
                    for (int index = 0; index < (nameItems.Length - 1); index++) {
                        if (string.IsNullOrEmpty(nameItems[index])) {
                            throw new InvalidJavaTypeException();
                        }
                        currentNamespace = currentNamespace.TryGetSon(nameItems[index]);
                        if (null == currentNamespace) {
                            return null;
                        }
                    }
                    string simpleName = nameItems[nameItems.Length - 1];
                    if (string.IsNullOrEmpty(simpleName)) {
                        throw new InvalidJavaTypeException();
                    }
                    if (null == (result = currentNamespace.TryGet(simpleName))) {
                        return null;
                    }
                    break;
                default:
                    throw new InvalidOperationException();
            }
            if (0 != builder.Length) {
                throw new InvalidJavaTypeException();
            }
            if (null == result) { return null; }
            while (0 < indirectionCount--) {
                result = NamingspaceItem.Root.TryGetArrayOf(result);
                if (null == result) { return null; }
            }
            return result;
        }

        private static void InitializeTypeSystem()
        {
            foreach(BuiltinType item in BuiltinType.EnumerateAll()) {
                // Nothing to do. Enumerating types will ensure everything is up.
            }
            return;
        }
        #endregion

        #region FIELDS
        public const char NamespaceItemSeparator = '/';
        private string _fullyQualifiedName;
        #endregion

        #region INNER CLASSES
        public class NamingspaceItem : INamespace
        {
            static NamingspaceItem()
            {
                Root = new NamingspaceItem() {
                    _arraysByIndexedItem =
                        new Dictionary<JavaTypeDefinition, JavaTypeDefinition>()
                };
            }

            private NamingspaceItem()
            {
                return;
            }

            private NamingspaceItem(string name)
            {
                if (string.IsNullOrEmpty(name)) { throw new ArgumentNullException(); }
                Name = name;
            }

            internal JavaTypeDefinition this[string name]
            {
                get
                {
                    JavaTypeDefinition result;
                    if(_typesPerName.TryGetValue(name, out result)) {
                        return result;
                    }
                    throw new InvalidOperationException();
                }
            }

            public bool IsRoot
            {
                get { return object.ReferenceEquals(this, Root); }
            }

            public string Name { get; private set; }
            internal NamingspaceItem Parent { get; private set; }
            INamespace INamespace.Parent
            {
                get { return this.Parent; }
            }
            internal static NamingspaceItem Root { get; private set; }

            internal NamingspaceItem GetOrCreateSon(string name)
            {
                NamingspaceItem result = TryGetSon(name);
                if (null != result) { return result; }
                result = new NamingspaceItem(name) {
                    Parent = this
                };
                if (null == _sonsByName) {
                    _sonsByName = new Dictionary<string, NamingspaceItem>();
                }
                _sonsByName.Add(name, result);
                return result;
            }

            internal NamingspaceItem GetSon(string name)
            {
                if (string.IsNullOrEmpty(name)) { throw new ArgumentNullException(); }
                NamingspaceItem result = TryGetSon(name);
                if (null != result) { return result; }
                throw new ApplicationException();
            }

            internal void Register(JavaTypeDefinition definition)
            {
                if (null == definition) { throw new ArgumentNullException(); }
                if (definition.IsArray) {
                    if (!object.ReferenceEquals(Root, this)) {
                        throw new InvalidOperationException();
                    }
                    this._arraysByIndexedItem.Add(definition, definition.IndexedType);
                }
                if (null == _typesPerName) {
                    _typesPerName = new Dictionary<string, JavaTypeDefinition>();
                }
                if (_typesPerName.ContainsKey(definition.Name)) {
                    throw new InvalidOperationException();
                }
                if (!object.ReferenceEquals(this, definition.Namespace)) {
                    throw new InvalidOperationException();
                }
                _typesPerName.Add(definition.Name, definition);
                return;
            }

            internal JavaTypeDefinition TryGet(string name)
            {
                JavaTypeDefinition result;
                return _typesPerName.TryGetValue(name, out result)
                    ? result
                    : null;
            }

            internal JavaTypeDefinition TryGetArrayOf(JavaTypeDefinition item)
            {
                if (null == item) { throw new ArgumentNullException(); }
                if (!object.ReferenceEquals(this, Root)) {
                    throw new InvalidOperationException();
                }
                JavaTypeDefinition result;
                return _arraysByIndexedItem.TryGetValue(item, out result)
                    ? result
                    : null;
            }

            internal NamingspaceItem TryGetSon(string name)
            {
                if (string.IsNullOrEmpty(name)) { throw new ArgumentNullException(); }
                if (null == _sonsByName) { return null; }
                NamingspaceItem result;
                return _sonsByName.TryGetValue(name, out result)
                    ? result
                    : null;
            }

            private Dictionary<JavaTypeDefinition, JavaTypeDefinition> _arraysByIndexedItem;
            private Dictionary<string, NamingspaceItem> _sonsByName;
            private Dictionary<string, JavaTypeDefinition> _typesPerName;
        }
        #endregion
    }
}
