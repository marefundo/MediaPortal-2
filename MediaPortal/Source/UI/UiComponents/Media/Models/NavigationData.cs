#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Views;
using MediaPortal.UiComponents.Media.Models.ScreenData;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Corresponds to the current media navigation step. The navigation data basically specifies the current underlaying set of
  /// media items, which are presented in one of the <see cref="AvailableScreens"/>, represented to the user as
  /// <see cref="DynamicWorkflowActions"/> in the menu.
  /// The <see cref="CurrentScreenData"/> holds the concrete data for that representation mode of the current navigation
  /// position, i.e. it provides the concrete UI data for the skin.
  /// </summary>
  public class NavigationData
  {
    public const string FILTERS_WORKFLOW_CATEGORY = "a-Filters";

    #region Protected properties

    protected string _navigationContextName;
    protected Guid _currentWorkflowStateId;
    protected Guid _baseWorkflowStateId;
    protected ViewSpecification _baseViewSpecification;
    protected AbstractScreenData _currentScreenData;
    protected ICollection<AbstractScreenData> _availableScreens;
    protected ICollection<WorkflowAction> _dynamicWorkflowActions;

    #endregion

    /// <summary>
    /// Creates a new navigation data structure for a new media navigation step.
    /// </summary>
    /// <param name="navigationContextName">Name, which is used for the corresponding workflow navigation context.</param>
    /// <param name="currentWorkflowStateId">Id of the workflow state which corresponds to the new media navigation step.</param>
    /// <param name="baseWorkflowStateId">Id of the workflow state to which the workflow navigation should be reverted when
    /// another filter is choosen.</param>
    /// <param name="baseViewSpecification">View specification for the media items of the new media navigation step.</param>
    /// <param name="defaultScreen">Screen which should present the new navigation step by default.</param>
    /// <param name="availableScreens">Available set of screen descriptions which can present the new media navigation step.</param>
    public NavigationData(string navigationContextName, Guid baseWorkflowStateId, Guid currentWorkflowStateId,
        ViewSpecification baseViewSpecification, AbstractScreenData defaultScreen, ICollection<AbstractScreenData> availableScreens)
    {
      _navigationContextName = navigationContextName;
      _currentWorkflowStateId = currentWorkflowStateId;
      _baseWorkflowStateId = baseWorkflowStateId;
      _baseViewSpecification = baseViewSpecification;
      _currentScreenData = defaultScreen;
      _availableScreens = availableScreens ?? new List<AbstractScreenData>();
      BuildWorkflowActions();
    }

    public void Dispose()
    {
      _currentScreenData.ReleaseScreenData();
    }

    public ViewSpecification BaseViewSpecification
    {
      get { return _baseViewSpecification; }
    }

    /// <summary>
    /// Returns the id of the workflow state to which we must revert when a different view presentation screen is choosen.
    /// </summary>
    public Guid BaseWorkflowStateId
    {
      get { return _baseWorkflowStateId; }
    }

    /// <summary>
    /// Returns the id of the corresponding workflow state.
    /// </summary>
    public Guid CurrentWorkflowStateId
    {
      get { return _currentWorkflowStateId; }
    }

    public AbstractScreenData CurrentScreenData
    {
      get { return _currentScreenData; }
      internal set { _currentScreenData = value; }
    }

    public ICollection<AbstractScreenData> AvailableScreens
    {
      get { return _availableScreens; }
    }

    /// <summary>
    /// Gets a collection of workflow actions to be shown in the menu which reflect the current
    /// <see cref="AvailableScreens"/>.
    /// </summary>
    public ICollection<WorkflowAction> DynamicWorkflowActions
    {
      get { return _dynamicWorkflowActions; }
    }

    /// <summary>
    /// Releases resources which are needed by the current screen.
    /// </summary>
    public void Disable()
    {
      _currentScreenData.ReleaseScreenData();
    }

    /// <summary>
    /// Restores resources which are needed by the current screen.
    /// </summary>
    public void Enable()
    {
      _currentScreenData.CreateScreenData(this);
    }

    protected void BuildWorkflowActions()
    {
      _dynamicWorkflowActions = new List<WorkflowAction>(_availableScreens.Count);
      int ct = 0;
      foreach (AbstractScreenData screen in _availableScreens)
      {
        AbstractScreenData newScreen = screen; // Necessary to be used in closure
        WorkflowAction action = new MethodDelegateAction(Guid.NewGuid(),
            _navigationContextName + "->" + newScreen.MenuItemLabel, _currentWorkflowStateId,
            LocalizationHelper.CreateResourceString(newScreen.MenuItemLabel), () =>
              {
                _currentScreenData.ReleaseScreenData();
                _currentScreenData = newScreen;
                _currentScreenData.CreateScreenData(this);
                IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
                // The last screen could have stepped into a deeper media navigation context when it had produced
                // sub views. But when a different screen is choosen to represent the underlaying view, we
                // need to revert our workflow to the base workflow id before going into the new screen.
                if (!workflowManager.NavigatePopToState(_baseWorkflowStateId, false))
                  // If the WF manager didn't change the state, still update the screen
                  SwitchToCurrentScreen();
              })
          {
              DisplayCategory = FILTERS_WORKFLOW_CATEGORY,
              SortOrder = ct++.ToString(), // Sort in the order we have built up the filters
          };
        _dynamicWorkflowActions.Add(action);
      }
    }

    protected void SwitchToCurrentScreen()
    {
      ServiceRegistration.Get<IScreenManager>().ShowScreen(_currentScreenData.Screen);
    }
  }
}