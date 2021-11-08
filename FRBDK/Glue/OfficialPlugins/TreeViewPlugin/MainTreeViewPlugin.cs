﻿using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using OfficialPlugins.TreeViewPlugin.Views;
using PropertyTools.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin
{
    [Export(typeof(PluginBase))]
    class MainTreeViewPlugin : PluginBase
    {
        public override string FriendlyName => "Tree View Plugin";

        public override Version Version => new Version(1, 0);

        MainTreeViewViewModel MainViewModel = new MainTreeViewViewModel();

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            var mainView = new MainTreeViewControl();


            mainView.DataContext = MainViewModel;

            AssignEvents();

            CreateAndAddTab(mainView, "Explorer (beta)", TabLocation.Left);
        }

        private void AssignEvents()
        {
            ReactToLoadedGluxEarly += HandleGluxLoaded;
            RefreshTreeNodeFor += HandleRefreshTreeNodeFor;
        }

        private void HandleGluxLoaded()
        {
            MainViewModel.AddDirectoryNodes();
            MainViewModel.RefreshGlobalContentTreeNodes();
        }

        private void HandleRefreshTreeNodeFor(GlueElement element)
        {
            MainViewModel.RefreshTreeNodeFor(element);
            
        }
    }
}
