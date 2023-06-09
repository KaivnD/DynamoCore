﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dynamo.Wpf.Interfaces
{
    /// <summary>
    /// Represents the specification for the library items layout in library view.
    /// </summary>
    public class LayoutSpecification
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public LayoutSpecification()
        {
            sections = new List<LayoutSection>();
        }

        /// <summary>
        /// List of LayoutSections
        /// </summary>
        public List<LayoutSection> sections { get; set; }

        /// <summary>
        /// Serializes the layout specification to json stream
        /// </summary>
        /// <returns></returns>
        public Stream ToJSONStream()
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            var serializer = new JsonSerializer() { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore };
            serializer.Serialize(sw, this);

            sw.Flush();
            ms.Position = 0;
            return ms;
        }

        /// <summary>
        /// Creates LibraryLayoutSpec object from the given json text
        /// </summary>
        /// <param name="jsonText">JSON text to deserialize</param>
        /// <returns>LibraryLayoutSpec object or null</returns>
        public static LayoutSpecification FromJSONString(string jsonText)
        {
            return JsonConvert.DeserializeObject<LayoutSpecification>(jsonText);
        }

        /// <summary>
        /// Creates LibraryLayoutSpec object from given json stream
        /// </summary>
        /// <param name="stream">JSON stream</param>
        /// <returns>LibraryLayoutSpec object or null</returns>
        public static LayoutSpecification FromJSONStream(Stream stream)
        {
            //Ensure that the stream is positioned at the beginning.
            stream.Position = 0;

            var sr = new StreamReader(stream);
            return FromJSONString(sr.ReadToEnd());
        }

        /// <summary>
        /// Performs a deep clone of this object and returns a new LayoutSpecification object
        /// </summary>
        /// <returns>A cloned LayoutSpecification object</returns>
        public LayoutSpecification Clone()
        {
            using (var s = ToJSONStream())
            {
                return FromJSONStream(s);
            }
        }

        /// <summary>
        /// Merge two layout specs.
        /// </summary>
        /// <param name="mainSpec">Original spec, will be cloned before modifications are made.</param>
        /// <param name="specToMerge">This spec will be merged into mainspec.</param>
        /// <returns>A modified spec with specToMerge merged into mainSpec. mainSpec object will not be modified.</returns>
        internal static LayoutSpecification MergeLayoutSpecs(LayoutSpecification mainSpec, LayoutSpecification specToMerge)
        {
            var mainspecClone = mainSpec.Clone();
            var originalSections = mainspecClone.sections;
            foreach(var sectionToMerge in specToMerge.sections)
            {
                var match = false;
                foreach (var orgSection in originalSections)
                {
                    //found a match
                    if(sectionToMerge.text == orgSection.text)
                    {
                        match = true;
                        MergeLayoutElements(orgSection, sectionToMerge, LayoutElementType.category);
                        break;
                    }
                }
                if (!match)
                {
                    mainspecClone.sections.Add(sectionToMerge);
                }
            }
            return mainspecClone;
        }
        /// <summary>
        /// Recursive function to merge one layout element into another based on element.text and type for matching.
        /// Will not merge any information between elements (include info, path etc) will only move child elements.
        /// </summary>
        /// <param name="orgElement"></param>
        /// <param name="mergeElement"></param>
        /// <param name="type"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void MergeLayoutElements(LayoutElement orgElement, LayoutElement mergeElement, LayoutElementType type)
        {
            //sanity check
            if (orgElement.text != mergeElement.text)
            {
                throw new ArgumentException("elements to merge must have the same name");
            }
            //find matching type
            foreach (var mergeChildElement in mergeElement.childElements.Where(x => x.elementType == type))
            {
                var match = false;
                foreach (var orgChildElement in orgElement.childElements.Where(x => x.elementType == type))
                {
                    //matching name.
                    if (orgChildElement.text == mergeChildElement.text)
                    {
                        match = true;
                        //if there is a match, recurse.
                        MergeLayoutElements(orgChildElement, mergeChildElement, LayoutElementType.group);
                        break;
                    }

                }
                //no match, move element to other spec.
                if (!match)
                {
                    Debug.WriteLine($"creating a new {type}:{mergeChildElement.text} under {orgElement.text} ");
                    orgElement.childElements.Add(mergeChildElement);
                }
            }
        }
    }

    /// <summary>
    /// Represents the information about the data type that needs to be included
    /// under a given LayoutElement.
    /// </summary>
    public class LayoutIncludeInfo
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public LayoutIncludeInfo()
        {
            inclusive = true;
        }

        /// <summary>
        /// Partial or full namespace to be included under a given layout element
        /// </summary>
        public string path { get; set; }

        /// <summary>
        /// Relative or absolute URL of the icon for the corresponding library item
        /// </summary>
        public string iconUrl { get; set; }

        /// <summary>
        /// Checks if a new category to be created for its path. If the include 
        /// path is "A.B.C" under an element say "Root" and for two nodes 
        /// fully qualified name is "A.B.C.D" and "A.B.C.E", then by default 
        /// inclusive is true that means there we will get a category "C" under 
        /// "Root" element as parent and "D" and "E" as children. If inclusive 
        /// is false then both "D" and "E" will be direct children of "Root" and 
        /// there won't be any intermediate element "C".
        /// </summary>
        public bool inclusive { get; set; }
    }

    /// <summary>
    /// Represents the type of the element. Possible values are section, 
    /// category, group, create, action, query and none.
    /// </summary>
    public enum LayoutElementType
    {
        /// <summary>
        /// Section elements represent the root items on the library view
        /// </summary>
        section,

        /// <summary>
        /// Category elements represent the root library items contained in section elements.
        /// </summary>
        category,

        /// <summary>
        /// Groups comes directly under its parent category, it contains just text without icon
        /// </summary>
        group,

        /// <summary>
        /// Elements of this type result in library items that get clubbed under the Create cluster
        /// </summary>
        create,

        /// <summary>
        /// Elements of this type result in library items that get clubbed under the Action cluster
        /// </summary>
        action,

        /// <summary>
        /// Elements of this type result in library items that get clubbed under the Query cluster
        /// </summary>
        query,

        /// <summary>
        /// All other expandable library items that are not categories or groups
        /// </summary>
        none,

        /// <summary>
        /// classType comes directly under its parent category, it contains just text without icon
        /// </summary>
        classType,
    }

    /// <summary>
    /// Represents a group/category
    /// </summary>
    public class LayoutElement
    {
        /// <summary>
        /// Name of the element
        /// </summary>
        public string text { get; set; }

        /// <summary>
        /// Relative or absolute URL of the icon for the corresponding library item
        /// </summary>
        public string iconUrl { get; set; }

        /// <summary>
        /// The type of the element.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public LayoutElementType elementType { get; set; }

        /// <summary>
        /// List of data types that should be included under this given library element
        /// </summary>
        public List<LayoutIncludeInfo> include { get; set; }

        /// <summary>
        /// List of nested elements under this element
        /// </summary>
        public List<LayoutElement> childElements { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="text">Text value of the element</param>
        public LayoutElement(string text)
        {
            this.text = text;
            childElements = new List<LayoutElement>();
            include = new List<LayoutIncludeInfo>();
            elementType = LayoutElementType.none;
        }

        /// <summary>
        /// Returns a flat list of all the include info, including those owned by 
        /// its child elements recursively.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<LayoutIncludeInfo> EnumerateIncludes()
        {
            var list = include.AsEnumerable();
            foreach (var item in childElements)
            {
                list = list.Concat(item.EnumerateIncludes());
            }

            return list;
        }

        /// <summary>
        /// Returns a flat list of all the children, including the children owned
        /// by its child elements recursively
        /// </summary>
        /// <returns></returns>
        public IEnumerable<LayoutElement> EnumerateChildren()
        {
            var list = childElements.AsEnumerable();
            foreach (var item in childElements)
            {
                list = list.Concat(item.EnumerateChildren());
            }

            return list;
        }
    }

    /// <summary>
    /// Represents a section element, which is the root element in the library view
    /// </summary>
    public class LayoutSection : LayoutElement
    {
        /// <summary>
        /// Sections may have headers displayed or hidden. If the header is shown
        /// the section is collapsible, otheriwse its always expanded.
        /// </summary>
        public bool showHeader { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="text">Text value of the element</param>
        public LayoutSection(string text) : base(text)
        {
            elementType = LayoutElementType.section;
            showHeader = true;
        }
    }

}
