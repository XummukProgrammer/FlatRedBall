﻿using FlatRedBall.Glue;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.TreeViewPlugin.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    public class NodeViewModel : ViewModel
    {

        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);

        #region Fields/Properties

        public object Tag { get; set; }
        
        public NodeViewModel Parent { get; private set; }

        public bool HasItems
        {
            get
            {
                //this.LoadChildren();
                return this.children.Count > 0;
            }
        }

        public void Detach()
        {
            this.Parent.Children.Remove(this);
            this.Parent = null;
        }

        //private Node Node;


        private ObservableCollection<NodeViewModel> children = new ObservableCollection<NodeViewModel>();

        public ObservableCollection<NodeViewModel> Children
        {
            get
            {
                //this.LoadChildren();
                return children;
            }
        }

        public string Text 
        {
            get => Get<string>();
            set
            {
                //this.Node.Name = value;
                Set(value);
            }
        }

        public bool IsExpanded
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int Level
        {
            get => Get<int>();
            set => Set(value);
        }

        #endregion

        public virtual void RefreshTreeNodes()
        {

        }

        //private void LoadChildren()
        //{
        //    if (children == null)
        //    {
        //        children = new ObservableCollection<NodeViewModel>();
        //        var cc = this.Node as CompositeNode;
        //        if (cc != null)
        //        {
        //            foreach (var child in cc.Children)
        //            {
        //                // Debug.WriteLine("Creating VM for " + child.Name);
        //                children.Add(new NodeViewModel(child, this));
        //                // Thread.Sleep(1);
        //            }
        //        }
        //    }
        //}

        public NodeViewModel(NodeViewModel parent)
        {
            //this.Node = Node;
            this.Parent = parent;
            this.IsExpanded = true;
        }

        public NodeViewModel AddChild()
        {
            //var cn = this.Node as CompositeNode;
            //if (cn == null)
            //{
            //    return null;
            //}

            //var newChild = new CompositeNode() { Name = "New node" };
            //cn.Children.Add(newChild);
            var vm = new NodeViewModel(this);
            this.Children.Add(vm);
            return vm;
        }

        public string GetRelativePath()
        {

            #region Directory tree node
            if (IsDirectoryNode())
            {
                if (Parent.IsRootEntityNode())
                {
                    return "Entities/" + Text + "/";

                }
                if (Parent.IsRootScreenNode())
                {
                    return "Screens/" + Text + "/";

                }
                else if (Parent.IsGlobalContentContainerNode())
                {

                    string contentDirectory = ProjectManager.MakeAbsolute("GlobalContent", true);

                    string returnValue = contentDirectory + Text;
                    if (IsDirectoryNode())
                    {
                        returnValue += "/";
                    }
                    // But we want to make this relative to the project, so let's do that
                    returnValue = ProjectManager.MakeRelativeContent(returnValue);

                    return returnValue;
                }
                else
                {
                    // It's a tree node, so make it have a "/" at the end
                    return Parent.GetRelativePath() + Text + "/";
                }
            }
            #endregion

            #region Global content container

            else if (IsGlobalContentContainerNode())
            {
                var returnValue = GlueState.Self.Find.GlobalContentFilesPath;


                // But we want to make this relative to the project, so let's do that
                returnValue = ProjectManager.MakeRelativeContent(returnValue);



                return returnValue;
            }
            #endregion

            else if (IsFilesContainerNode())
            {
                string valueToReturn = Parent.GetRelativePath();


                return valueToReturn;
            }
            else if (IsFolderInFilesContainerNode())
            {
                return Parent.GetRelativePath() + Text + "/";
            }
            else if (IsElementNode())
            {
                return ((IElement)Tag).Name + "/";
            }
            else if (IsReferencedFile())
            {
                string toReturn = Parent.GetRelativePath() + Text;
                toReturn = toReturn.Replace("/", "\\");
                return toReturn;
            }
            else
            {
                // Improve this to handle embeded stuff
                string textToReturn = Text;

                if (string.IsNullOrEmpty(FlatRedBall.IO.FileManager.GetExtension(textToReturn)))
                {
                    textToReturn += "/";
                }

                return textToReturn;
            }
        }

        // Teh "Is" methods are added to make refactoring easier. Not sure if we eventually want to get rid of them:
        public bool IsDirectoryNode()
        {
            if (Parent == null)
            {
                return false;
            }

            if (this is GlueElementNodeViewModel)
                return false;

            if (Tag != null)
            {
                return false;
            }

            if (Parent.IsRootEntityNode() || Parent.IsGlobalContentContainerNode())
                return true;


            if (Parent.IsFilesContainerNode() || Parent.IsDirectoryNode())
            {
                return true;
            }

            else
                return false;
        }

        public bool IsRootEntityNode() => Text == "Entities" && Parent == null;
        public bool IsRootScreenNode() => Text == "Screens" && Parent == null;
        

        public bool IsEntityNode()
        {
            return Tag is EntitySave;
        }

        public bool IsScreenNode() => Tag is ScreenSave;

        public bool IsGlobalContentContainerNode()
        {
            return Text == "Global Content Files" && Parent == null;
        }

        public bool IsFilesContainerNode()
        {
            var parentTreeNode = Parent;
            return Text == "Files" && parentTreeNode != null &&
                (parentTreeNode.IsEntityNode() || parentTreeNode.IsScreenNode());
        }

        public bool IsFolderInFilesContainerNode()
        {
            var parentTreeNode = Parent;

            return Tag == null && parentTreeNode != null &&
                (parentTreeNode.IsFilesContainerNode() || parentTreeNode.IsFolderInFilesContainerNode());

        }

        public bool IsElementNode() => Tag is GlueElement;
        public bool IsReferencedFile() => Tag is ReferencedFileSave;

        public void SortByTextConsideringDirectories(ObservableCollection<NodeViewModel> treeNodeCollection = null, bool recursive = false)
        {
            if(treeNodeCollection == null)
            {
                treeNodeCollection = Children;
            }

            int lastObjectExclusive = treeNodeCollection.Count;
            int whereObjectBelongs;
            for (int i = 0 + 1; i < lastObjectExclusive; i++)
            {
                var first = treeNodeCollection[i];
                var second = treeNodeCollection[i - 1];
                if (TreeNodeComparer(first, second) < 0)
                {
                    if (i == 1)
                    {
                        var treeNode = treeNodeCollection[i];
                        treeNodeCollection.RemoveAt(i);

                        treeNodeCollection.Insert(0, treeNode);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        second = treeNodeCollection[whereObjectBelongs];
                        if (TreeNodeComparer(treeNodeCollection[i], second) >= 0)
                        {
                            var treeNode = treeNodeCollection[i];

                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(whereObjectBelongs + 1, treeNode);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && TreeNodeComparer(treeNodeCollection[i], treeNodeCollection[0]) < 0)
                        {
                            var treeNode = treeNodeCollection[i];
                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(0, treeNode);
                            break;
                        }
                    }
                }
            }

            if (recursive)
            {
                foreach (var node in treeNodeCollection)
                {
                    if (node.IsDirectoryNode())
                    {
                        SortByTextConsideringDirectories(node.Children, recursive);
                    }
                }
            }

        }

        private static int TreeNodeComparer(NodeViewModel first, NodeViewModel second)
        {
            bool isFirstDirectory = first.IsDirectoryNode();
            bool isSecondDirectory = second.IsDirectoryNode();

            if (isFirstDirectory && !isSecondDirectory)
            {
                return -1;
            }
            else if (!isFirstDirectory && isSecondDirectory)
            {
                return 1;
            }
            else
            {

                //return first.Text.CompareTo(second.Text);
                // This will put Level9 before Level10
                return StrCmpLogicalW(first.Text, second.Text);
            }
        }

        public NodeViewModel Root() => Parent == null ? this : Parent.Root();
    }
}
