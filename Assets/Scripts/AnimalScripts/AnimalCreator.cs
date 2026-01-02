using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;


[System.Serializable]
public class SegmentData
{
    [Header("Ustawienia ³añcucha")]
    public string segmentName;
    public int jointCount;
    public AnimationCurve sizeCurve;
    public float distanceConstraint;

    [Header("Ustawienia zakresu wygiêcia")]
    [Range(0, 180)] public float angularConstraint;
    [Range(-180, 180)] public float prefferedAngle;
}

public class AnimalCreator : MonoBehaviour
{
    protected AnimalAnimator animatorScript;
    protected AnimalEventHub eventHub;

    [Header("Globalne ustawienia segmentów")]
    [SerializeField] private ScriptableCreator creatorData;
    [SerializeField] private Color bodyColor = Color.red;

    protected List<AnimalJoint> spineJoints = new List<AnimalJoint>();
    protected List<AnimalLimb> limbs = new List<AnimalLimb>();
    protected AnimalHead animalHead;

    private GameObject prefabCache;

    protected void Awake()
    {
        animatorScript = GetComponent<AnimalAnimator>();
        eventHub = GetComponent<AnimalEventHub>();
    }
    async void Start()
    {
        bool hasHead = false;
        await LoadPrefabs();
        if (prefabCache == null)
        {
            Debug.LogError("AnimalCreator: Nie znaleziono obiektu addressable AnimalJoint! Upewnij siê ¿e obiekt tego typu jest oznaczony jako Addressable! Zatrzymano proces generowania zwierzêcia.", this);
            return;
        }

        if (creatorData != null)
        {
            GenerateBody();
            if (creatorData.animalHeadData.joints.Count > 0)
            {
                GenerateHead();
                hasHead = true;
            }
            if (creatorData.animalLimbData.Count > 0)
                GenerateLimbs();
            if (creatorData.mouthColliderPrefab is null)
                Debug.LogWarning("AnimalCreator: Nie znaleziono prefabrykanta MouthCollider", this);

            animatorScript.SetBody(spineJoints, limbs, animalHead);

            if (creatorData.attachMouthToHeadSegment && hasHead)
            {
                if (hasHead)
                {
                    if (creatorData.mouthColliderPrefab == null)
                        Debug.LogWarning("AnimalCreator: Mouth Collider prefab jest pusty! Czy to zamierzone zachowanie?", this);
                    else
                    {
                        GameObject mouthCollider = Instantiate(creatorData.mouthColliderPrefab);
                        AttachMouthCollider(animalHead.headJoints, mouthCollider, creatorData.mouthParentId);
                        mouthCollider.GetComponent<AnimalMouthCollider>().OnInstantiate();
                    }
                }
                else
                    Debug.LogError("AnimalCreator: Próba przypisania otworu gêbowego do segmentu g³owy, kiedy jej nie ma! Wybierz prypisanie do segmentu cia³a jeœli jest to zamierzone!", this);
            }
            else
            {
                if (creatorData.mouthColliderPrefab != null)
                {
                    GameObject mouthCollider = Instantiate(creatorData.mouthColliderPrefab);
                    AttachMouthCollider(spineJoints, mouthCollider, creatorData.mouthParentId);
                    mouthCollider.GetComponent<AnimalMouthCollider>().OnInstantiate();
                }
            }
            eventHub.AnnounceBodyGenerated();
        }
        else
            Debug.LogError("AnimalCreator: Creator data jest nie przypisane albo jest puste!");
        ReleasePrefab();
    }

    protected void GenerateBody()
    {
        Transform masterTransform = transform;
        Vector3 positionOffset = Vector3.zero;
        int nameId = 0;

        foreach (SegmentData currSegmentData in creatorData.spineSegmentData)
        {
            for (int i = 0; i < currSegmentData.jointCount; i++)
            {
                float xValue = (float)i / (float)currSegmentData.jointCount;
                float segmentScale = currSegmentData.sizeCurve.Evaluate(xValue);
                string name = currSegmentData.segmentName + " Spine Segment " + nameId++;
                AnimalJoint newJoint = GenerateSegment(segmentData: currSegmentData, iteration: i, masterTransform, positionOffset, segmentScale, name);
                spineJoints.Add(newJoint);
                positionOffset += new Vector3(0, 0, -1f * segmentScale * currSegmentData.distanceConstraint);
            }
        }
    }

    protected void GenerateHead()
    {
        Transform masterTransform = transform;
        Vector3 positionOffset = creatorData.animalHeadData.headParentOffset;
        List<AnimalJoint> headJoints = new List<AnimalJoint>();

        int nameId = 0;
        foreach (SegmentData currSegmentData in creatorData.animalHeadData.joints)
        {
            for (int i = 0; i < currSegmentData.jointCount; i++)
            {
                float xValue = (float)i / (float)currSegmentData.jointCount;
                float segmentScale = currSegmentData.sizeCurve.Evaluate(xValue);
                string name = currSegmentData.segmentName + " Head Segment " + nameId++;
                headJoints.Add(GenerateSegment(currSegmentData, iteration: i, masterTransform, positionOffset, segmentScale, name));
                positionOffset += new Vector3(0, 0, 1f * segmentScale * currSegmentData.distanceConstraint);
            }
        }
        animalHead = new AnimalHead(headJoints, spineJoints[0], creatorData.animalHeadData);
    }

    protected void GenerateLimbs()
    {
        Transform masterTransform = transform;
        int limbId = 0;

        foreach (AnimalLimbData currLimbData in creatorData.animalLimbData)
        {
            Vector3 positionOffset = spineJoints[currLimbData.parentJointId].segmentPosition + currLimbData.parentPositionOffset - masterTransform.position;

            List<AnimalJoint> limbJoints = new List<AnimalJoint>();

            foreach (SegmentData currSegmentData in currLimbData.joints)
            {
                int nameId = 0;
                for (int i = 0; i < currSegmentData.jointCount; i++)
                {
                    float xValue = (float)i / (float)currSegmentData.jointCount;
                    float segmentScale = currSegmentData.sizeCurve.Evaluate(xValue);

                    string name = currLimbData.limbName + " " + currSegmentData.segmentName + " Segment " + nameId++;

                    limbJoints.Add(GenerateSegment(currSegmentData, iteration: i, masterTransform, positionOffset, segmentScale, name));
                    float offsetDirection = (currLimbData.parentPositionOffset.x >= 0f) ? 1 : -1;
                    positionOffset += new Vector3(offsetDirection * segmentScale * currSegmentData.distanceConstraint, 0, 0);
                }
            }
            limbs.Add(new AnimalLimb(currLimbData, limbJoints, spineJoints[currLimbData.parentJointId], limbId++));
        }
    }

    protected AnimalJoint GenerateSegment(SegmentData segmentData, int iteration, Transform masterTransform, Vector3 positionOffset, float segmentScale, string name)
    {
        GameObject newSegment = Instantiate(prefabCache, masterTransform);
        newSegment.transform.localScale = Vector3.one * segmentScale;
        newSegment.transform.position = masterTransform.position + masterTransform.rotation * positionOffset;
        newSegment.transform.rotation = masterTransform.rotation * prefabCache.transform.rotation;
        newSegment.name = name;

        newSegment.GetComponent<SpriteRenderer>().color = bodyColor;
        AnimalJoint segmentScript = newSegment.GetComponent<AnimalJoint>();
        segmentScript.AfterInstantiate(segmentData.distanceConstraint * segmentScale, segmentData.angularConstraint, segmentData.prefferedAngle, iteration);

        return segmentScript;
    }

    private async Task LoadPrefabs()
    {
        if (prefabCache == null)
        {
            prefabCache = await Addressables.LoadAssetAsync<GameObject>("AnimalJoint").Task;
        }
    }

    private void ReleasePrefab() { 
        if (prefabCache != null)
        {
            Addressables.Release(prefabCache);
            prefabCache = null;
        }
    }
    public void AttachMouthCollider(List<AnimalJoint> attachChain, GameObject mouthCollider, int segmentId)
    {
        if (mouthCollider == null)
        {
            Debug.Log("AnimalCreator: Nie mo¿na podpi¹æ MouthCollider - obiekt nie zosta³ sprecyzowany!");
            return;
        }
        if (segmentId < 0 || segmentId > attachChain.Count)
        {
            Debug.Log("AnimalCreator: Nie mo¿na podpi¹æ MouthCollider - podane id rodzica nie jest w zakresie id ³añcucha!");
            return;
        }

        Transform parent = attachChain[segmentId].transform;
        mouthCollider.transform.SetParent(parent, false);
        mouthCollider.transform.localPosition = Vector3.zero;
        mouthCollider.transform.localScale = Vector3.one;
    }
}
