﻿#region CodeMaid is Copyright 2007-2011 Steve Cadwallader.

// CodeMaid is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License version 3
// as published by the Free Software Foundation.
//
// CodeMaid is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details <http://www.gnu.org/licenses/>.

#endregion CodeMaid is Copyright 2007-2011 Steve Cadwallader.

using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SteveCadwallader.CodeMaid.CodeItems;
using SteveCadwallader.CodeMaid.CodeTree;

namespace SteveCadwallader.CodeMaid.Spade
{
    /// <summary>
    /// The Spade tool window pane.
    /// </summary>
    [Guid("75d09b86-471e-4b30-8720-362d13ad0a45")]
    public class SpadeToolWindow : ToolWindowPane, IVsWindowFrameNotify3
    {
        #region Fields

        private readonly SpadeCodeModelRetriever _codeModelRetriever;
        private readonly SpadeViewHost _viewHost;
        private readonly SpadeViewModel _viewModel;

        private Document _document;
        private bool _isVisible;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SpadeToolWindow"/> class.
        /// </summary>
        public SpadeToolWindow() :
            base(null)
        {
            // Set the tool window caption.
            Caption = "CodeMaid Spade";

            // Set the tool window image from resources.
            BitmapResourceID = 508;
            BitmapIndex = 0;

            // Create the toolbar for the tool window.
            ToolBar = new CommandID(GuidList.GuidCodeMaidToolbarSpadeBaseGroup, PkgCmdIDList.ToolbarIDCodeMaidToolbarSpade);

            // Setup the associated classes.
            _codeModelRetriever = new SpadeCodeModelRetriever(UpdateViewModelRawCodeItems);
            _viewModel = new SpadeViewModel();
            _viewHost = new SpadeViewHost(_viewModel);

            // Register for view model requests to be refreshed.
            _viewModel.RequestingRefresh += (sender, args) => Refresh();
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// Gets or sets the layout mode.
        /// </summary>
        public TreeLayoutMode LayoutMode
        {
            get { return _viewModel.LayoutMode; }
            set { _viewModel.LayoutMode = value; }
        }

        /// <summary>
        /// Retrieves the window associated with this window pane.
        /// </summary>
        public override IWin32Window Window
        {
            get { return _viewHost; }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// A method to be called to notify the tool window about the current active document.
        /// </summary>
        /// <param name="document">The active document.</param>
        public void NotifyActiveDocument(Document document)
        {
            Document = document;
        }

        /// <summary>
        /// A method to be called to notify the tool window that has a document has been saved.
        /// </summary>
        /// <param name="document">The document.</param>
        public void NotifyDocumentSave(Document document)
        {
            Refresh();
        }

        /// <summary>
        /// This method can be overriden by the derived class to execute
        /// any code that needs to run after the IVsWindowFrame is created.
        /// If the toolwindow has a toolbar with a combobox, it should make
        /// sure its command handler are set by the time they return from
        /// this method.
        /// This is called when someone set the Frame property.
        /// </summary>
        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();

            // Register for events to this window.
            ((IVsWindowFrame)Frame).SetProperty(
                (int)__VSFPROPID.VSFPROPID_ViewHelper, this);
        }

        /// <summary>
        /// Refresh the Spade tool window.
        /// </summary>
        public void Refresh()
        {
            ConditionallyUpdateCodeModel(true);
        }

        #endregion Public Methods

        #region Private Properties

        /// <summary>
        /// Gets or sets the current document.
        /// </summary>
        private Document Document
        {
            get { return _document; }
            set
            {
                if (_document != value)
                {
                    _document = value;
                    ConditionallyUpdateCodeModel(false);
                }
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating if this tool window is visible.
        /// </summary>
        private bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    ConditionallyUpdateCodeModel(false);
                }
            }
        }

        /// <summary>
        /// Gets or sets the package that owns the tool window.
        /// </summary>
        private new CodeMaidPackage Package { get { return base.Package as CodeMaidPackage; } }

        #endregion Private Properties

        #region Private Methods

        /// <summary>
        /// Conditionally updates the code model.
        /// </summary>
        /// <param name="isRefresh">True if refreshing a document, otherwise false.</param>
        private void ConditionallyUpdateCodeModel(bool isRefresh)
        {
            if (!IsVisible) return;

            // Late initialize the Package, not available during constructor.
            _viewModel.Package = Package;
            _viewModel.Document = Document;

            if (Document == null || !isRefresh)
            {
                _viewModel.RawCodeItems = null;
            }

            if (Document != null)
            {
                if (isRefresh)
                {
                    _viewModel.IsRefreshing = true;
                }
                else
                {
                    _viewModel.IsLoading = true;
                }

                _codeModelRetriever.RetrieveCodeModelAsync(Document);
            }
        }

        /// <summary>
        /// Updates the view model's raw set of code items.
        /// </summary>
        /// <param name="codeItems">The code items.</param>
        private void UpdateViewModelRawCodeItems(SetCodeItems codeItems)
        {
            _viewModel.RawCodeItems = codeItems;
            _viewModel.IsLoading = false;
            _viewModel.IsRefreshing = false;
        }

        #endregion Private Methods

        #region IVsWindowFrameNotify3 Members

        public int OnClose(ref uint pgrfSaveOptions)
        {
            return VSConstants.S_OK;
        }

        public int OnDockableChange(int fDockable, int x, int y, int w, int h)
        {
            return VSConstants.S_OK;
        }

        public int OnMove(int x, int y, int w, int h)
        {
            return VSConstants.S_OK;
        }

        public int OnShow(int fShow)
        {
            // Track the visibility of this tool window.
            switch ((__FRAMESHOW)fShow)
            {
                case __FRAMESHOW.FRAMESHOW_WinShown:
                    IsVisible = true;
                    break;

                case __FRAMESHOW.FRAMESHOW_WinHidden:
                    IsVisible = false;
                    break;
            }

            return VSConstants.S_OK;
        }

        public int OnSize(int x, int y, int w, int h)
        {
            return VSConstants.S_OK;
        }

        #endregion IVsWindowFrameNotify3 Members
    }
}