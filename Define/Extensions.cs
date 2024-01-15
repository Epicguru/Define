using System.Diagnostics;
using System.Text;
using System.Xml;

namespace Define;

/// <summary>
/// An assortment of extension methods for internal use in the Define library.
/// </summary>
public static class Extensions
{
    private static readonly StringBuilder str = new StringBuilder();

    /// <summary>
    /// Gets the string value of an attribute on this node, or returns the <paramref name="defaultValue"/>
    /// if the attribute was not found.
    /// </summary>
    public static string? GetAttributeValue(this XmlNode node, string attrName, string? defaultValue = null)
    {
        if (node is not XmlElement e)
            return null;

        var attr = e.Attributes[attrName];
        return attr == null ? defaultValue : attr.Value;
    }

    /// <summary>
    /// A version of <see cref="GetAttributeValue"/> that parses the value as a boolean.
    /// </summary>
    public static bool GetAttributeAsBool(this XmlNode node, string attrName, bool defaultValue = false)
    {
        string? value = node.GetAttributeValue(attrName);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets the underlying type of this nullable type.
    /// If this type is not nullable, the type itself is returned.
    /// </summary>
    public static Type StripNullable(this Type type) => Nullable.GetUnderlyingType(type) ?? type;

    /// <summary>
    /// Attempts to write the value of an attribute on this node.
    /// </summary>
    public static void SetAttribute(this XmlNode node, string name, string value)
    {
        if (node is not XmlElement e)
            return;

        var found = e.Attributes[name];
        if (found != null)
        {
            found.Value = value;
            return;
        }

        var created = e.OwnerDocument.CreateAttribute(name);
        created.Value = value;
        e.Attributes.Append(created);
    }

    /// <summary>
    /// Gets the full XPath for a particular <see cref="XmlNode"/> in a document.
    /// </summary>
    public static string GetFullXPath(this XmlElement node)
    {
        str.Clear();

        string NodeToString(XmlNode n)
        {
            if (n.ParentNode == null || n.ParentNode.ChildNodes.Count == 1)
                return n.Name;

            int self = -1;
            int i = 0;
            foreach (XmlNode sibling in n.ParentNode.ChildNodes)
            {
                if (sibling == n)
                {
                    self = i++;
                    continue;
                }

                if (sibling.NodeType == n.NodeType && sibling.Name == n.Name)
                {
                    i++;
                }
            }

            if (i > 1)
            {
                Debug.Assert(self != -1);
                return $"{n.Name}[{self + 1}]";
            }
            return n.Name;
        }

        XmlNode? current = node;
        while (current != null && current is not XmlDocument)
        {
            if (current != node)
                str.Insert(0, '/');
            str.Insert(0, NodeToString(current));
            current = current.ParentNode;
        }

        return str.ToString();
    }
}