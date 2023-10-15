﻿using SIPackages.Helpers;
using SIPackages.Models;
using System.Xml;

namespace SIPackages;

/// <summary>
/// Defines a question script as a sequence of steps.
/// </summary>
public sealed class Script : IEquatable<Script>
{
    /// <summary>
    /// Script steps which are executed sequentially.
    /// </summary>
    public List<Step> Steps { get; } = new();

    /// <inheritdoc />
    public override string ToString() => string.Join(" => ", Steps);

    /// <inheritdoc />
    public bool Equals(Script? other) => other is not null && Steps.SequenceEqual(other.Steps);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Script);

    /// <inheritdoc />
    public override int GetHashCode() => Steps.GetCollectionHashCode();

    /// <inheritdoc />
    public void ReadXml(XmlReader reader, PackageLimits? limits)
    {
        var read = true;

        while (!read || reader.Read())
        {
            read = true;

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    switch (reader.LocalName)
                    {
                        case "step":
                            if (limits == null || Steps.Count < limits.StepCount)
                            {
                                var step = new Step();
                                step.ReadXml(reader, limits);
                                Steps.Add(step);
                            }
                            else
                            {
                                reader.Skip();
                            }

                            read = false;
                            break;
                    }

                    break;

                case XmlNodeType.EndElement:
                    if (reader.LocalName == "script")
                    {
                        reader.Read();
                        return;
                    }
                    break;
            }
        }
    }

    /// <inheritdoc />
    public void WriteXml(XmlWriter writer)
    {
        foreach (var step in Steps)
        {
            writer.WriteStartElement("step");
            step.WriteXml(writer);
            writer.WriteEndElement();
        }
    }
}
