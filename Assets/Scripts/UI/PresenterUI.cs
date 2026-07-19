using System.Collections.Generic;
using UnityEngine;

public abstract class PresenterUI : MonoBehaviour
{
    private List<BaseViewUI> _views = new List<BaseViewUI>();

    protected List<BaseViewUI> views => _views;

    private BaseViewUI _currentView;

    protected BaseViewUI currentView => _currentView;

    protected void AddView(BaseViewUI view)
    {
        if (view != null && !_views.Contains(view))
        {
            _views.Add(view);
            view.Deactivate();
        }
    }

    protected void RemoveView(BaseViewUI view)
    {
        if (view != null && !_views.Contains(view))
        {
            _views.Remove(view);
        }
    }

    protected void ActivateView(BaseViewUI viewToActivate)
    {
        if (viewToActivate == null || !_views.Contains(viewToActivate))
            return;

        foreach (BaseViewUI view in _views)
        {
            if (view == viewToActivate)
            {
                _currentView = view;
                view.Activate();
            }
            else
            {
                view.Deactivate();
            }
        }
    }

    protected void DeactivateView(BaseViewUI viewToDeactivate)
    {
        if (viewToDeactivate == null || !_views.Contains(viewToDeactivate))
            return;

        foreach (BaseViewUI view in _views)
        {
            if (view == viewToDeactivate)
            {
                _currentView = view;
                view.Deactivate();
            }
        }
    }

    public abstract void Subscribe();

    public abstract void Unsubscribe();
}
