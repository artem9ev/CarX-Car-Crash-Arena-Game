using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class GameHUDView : BaseViewUI, IRaceHUDView
{
    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI m_timer;
    [SerializeField] private TextMeshProUGUI m_speed;

    [Header("Engine")]
    [SerializeField] private Speedometer m_rpmMeter;
    [SerializeField] private TextMeshProUGUI m_currentGear;

    private MatchResultsUi m_matchResults;



    public event UnityAction onPauseButtonClick;


    private void Awake()
    {
        m_matchResults = GetComponent<MatchResultsUi>();
    }
    public void SetSpeed(float speed)
    {
        if (m_speed == null)
        {
            return;
        }
        m_speed.text = Mathf.RoundToInt(speed * 3.6f).ToString();
    }

    public override void Deactivate()
    {
        base.Deactivate();

        //m_matchResults.
    }

    public void Deinit()
    {
        m_rpmMeter.Deinitialize();
    }

    public void SetTime(float time)
    {
        if (m_timer == null)
        {
            return;
        }
        
        m_timer.text = TimeConverter.GetTimer(time);
    }

    public void ResetTimer()
    {
        if (m_timer == null)
        {
            return;
        }

        m_timer.text = "00:00:00";
    }

    public void SetGear(int currentGear)
    {
        currentGear++;
        if (currentGear == -1)
        {
            m_currentGear.text = "R";
        }
        else if (currentGear == 0)
        {
            m_currentGear.text = "N";
        }
        else
        {
            m_currentGear.text = currentGear.ToString();
        }
    }

    public void SetUpRPMmeter(float maxRPM)
    {
        m_rpmMeter.Initialize(maxRPM);
    }
    
    public void SetRpm(float rpm)
    {
        m_rpmMeter.SetValue(rpm);
    }

    public void OnPauseClick()
    {
        onPauseButtonClick?.Invoke();
    }
}
