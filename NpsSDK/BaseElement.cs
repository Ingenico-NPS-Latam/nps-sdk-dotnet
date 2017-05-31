using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NpsSDK
{
    public abstract class BaseElement
    {
        //protected BaseElement(String name) { Name = name; }

        internal String Name { get; set; }
        public abstract String Serialize();
        internal abstract List<BaseElement> Children { get; }
        protected internal abstract String ConcatenatedValues { get; }

    }

    public class SimpleElement : BaseElement
    {
        private String Value { get; set; }
        public override string Serialize() { return String.Format("<{0}>{1}</{0}>", Name, Value); }

        internal void Trim(Int32 maxLength)
        {
            if (Value.Length > maxLength)
            {
                Value = Value.Substring(0, maxLength);
            }
        }

        private static readonly List<BaseElement> EmptyChildren = new List<BaseElement>();
        internal override List<BaseElement> Children { get { return EmptyChildren; } }
        protected internal override string ConcatenatedValues { get { return Value; } }

        public SimpleElement(string name, string value) { Name = name; Value = value; }
    }

    public class ComplexElement : BaseElement, IEnumerable<BaseElement>
    {
        private readonly Dictionary<String, BaseElement> _childrenHash;
        private readonly List<BaseElement> _children;
        internal override List<BaseElement> Children { get { return _children; } }
        public override string Serialize() { return String.Format("<{0}>{1}</{0}>", Name, String.Join("", Children.Select(x => x.Serialize()))); }
        protected internal override string ConcatenatedValues { get { return String.Join("", Children.OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => x.ConcatenatedValues)); } }

        public IEnumerator<BaseElement> GetEnumerator() { return Children.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public ComplexElement()
        {
            _childrenHash = new Dictionary<String, BaseElement>();
            _children = new List<BaseElement>();
        }

        //public void Add(BaseElement baseElement) { _children.Add(baseElement.Name, baseElement); }
        public void Add(String name, String value)
        {
            var simpleElement = new SimpleElement(name, value);
            _childrenHash.Add(name, simpleElement);
            _children.Add(simpleElement);
        }
        public void Add(String name, BaseElement baseElement)
        {
            baseElement.Name = name;
            _childrenHash.Add(name, baseElement);
            _children.Add(baseElement);
        }

        public BaseElement this[String index] { get { return _childrenHash.ContainsKey(index) ? _childrenHash[index] : null; } }
        public String GetValue(String index) { var child = this[index]; return child != null && child is SimpleElement ? child.ConcatenatedValues : null; }
        public T GetChild<T>(String index) where T : BaseElement { var child = this[index]; return child as T; }
        public ComplexElement GetComplexElement(String index) { return GetChild<ComplexElement>(index); }
        public ComplexElementArray GetComplexElementArray(String index) { return GetChild<ComplexElementArray>(index); }
        public SimpleElementArray GetSimpleElementArray(String index) { return GetChild<SimpleElementArray>(index); }
    }

    public class SimpleElementArray : BaseElement, IEnumerable<String>
    {
        private List<String> Values { get; set; }
        public override string Serialize() { return String.Format("<{0}>{1}</{0}>", Name, String.Join(",", Values)); }
        protected internal override string ConcatenatedValues { get { return String.Join(",", Values); } }

        private static readonly List<BaseElement> EmptyChildren = new List<BaseElement>();
        internal override List<BaseElement> Children { get { return EmptyChildren; } }

        public IEnumerator<String> GetEnumerator() { return Values.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        //public SimpleElementArray(string name) : base(name) { }

        public void Add(String value) { Values.Add(value); }

        public String this[Int32 index] { get { return Values[index]; } }
    }

    public class ComplexElementArray : BaseElement, IReadOnlyCollection<ComplexElementArrayItem>
    {
        internal String ChildType { set; private get; }
        public ComplexElementArray()
        {
            _children = new List<BaseElement>();
            _typedChildren = new List<ComplexElementArrayItem>();
        }

        private readonly List<BaseElement> _children;
        internal override List<BaseElement> Children { get { return _children; } }
        private readonly List<ComplexElementArrayItem> _typedChildren;

        public override string Serialize() { return String.Format("<{0} q2:arrayType=\"q3:{1}[{2}]\">{3}</{0}>", Name, ChildType, Children.Count, String.Join("", Children.Select(x => String.Format("<item xsi:type=\"q1:{0}\">{1}</item>", ChildType, x.Serialize())))); }
        protected internal override string ConcatenatedValues { get { return String.Join("", Children.OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => x.ConcatenatedValues)); } }

        public IEnumerator<ComplexElementArrayItem> GetEnumerator() { return _typedChildren.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public void Add(ComplexElementArrayItem complexElementArrayItem)
        {
            _typedChildren.Add(complexElementArrayItem);
            Children.Add(complexElementArrayItem);
        }

        public ComplexElementArrayItem this[Int32 index] => _typedChildren[index];

        public int Count { get { return _typedChildren.Count; } }
    }

    public class ComplexElementArrayItem : ComplexElement
    {
        //public ComplexElementArrayItem() : base(String.Empty) { }

        public override string Serialize() { return String.Join("", Children.Select(x => x.Serialize())); }
    }

    public class RootElement : ComplexElement
    {
        //public RootElement() : base(String.Empty) { }

        private string SecureHashConcatenatedValues { get { return String.Join("", Children.Where(x => x is SimpleElement).Cast<SimpleElement>().OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => x.ConcatenatedValues)); } }

        public override string Serialize() { return String.Join("", Children.Select(x => x.Serialize())); }

        public String SecureHash(String secretKey)
        {
            return GetMd5Hash(SecureHashConcatenatedValues + secretKey);
        }

        private static readonly MD5 Md5 = MD5.Create();
        private static string GetMd5Hash(string input)
        {
            byte[] data = Md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            foreach (byte t in data) { sBuilder.Append(t.ToString("x2")); }
            return sBuilder.ToString();
        }
    }
}
