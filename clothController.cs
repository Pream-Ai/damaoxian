using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class clothController : MonoBehaviour
{
    private Material this_material;
    private List<Vector3> pivotList = new List<Vector3>();
    public float rad;
    public List<Transform> path = new List<Transform>();
    Transform clothPool;
    public bool canBeClick = true;
    private bool isRoll = false;
    public int modeIndex;
    public int color;
    private ropeController rope;
    private yarnBallController yarnball;
    private Tweener blacknessTweener;
    public int occlusionCount = 0;
    void Start()
    {
        transform.GetComponent<SpriteRenderer>().material = GameManager.instance.clothMaterial[modeIndex];
        this_material = transform.GetComponent<SpriteRenderer>().material;
        clothPool = GameObject.Find("Cloth").transform;
    }
    public void beClicked()
    {
        if (colliderCheck())
        {
            if (!canBeClick) return;
            if (isRoll) return;
            canBeClick = false;
            isRoll = true;
            AudioManager.instance.clickSound();
            AudioManager.instance.ropeSound();
            transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().DOFade(0,0.2f);
            yarnTopPathGenerate();
            StopAllCoroutines();
            StartCoroutine(dissolveTrigger());

            transform.GetComponent<PolygonCollider2D>().enabled = false;
            var result = ropeGenerate();
            rope = result.Item1;
            rope.color = color;
            rope.initRope(color);
            rope.LineMove(pivotList);
            yarnball = result.Item2;
            yarnball.beClick();
            canBeClick = true;

            occlusionCount = -1;
            for (int i = 0; i < clothPool.childCount; i++)
            {
                var other = clothPool.GetChild(i);
                if (other == transform) continue;
                var otherController = other.GetComponent<clothController>();
                if (otherController != null &&
                    other.position.z > transform.position.z &&
                    Vector3.Distance(other.position, transform.position) < 3f)
                {
                    otherController.UpdateBlackness(-1);
                }
            }
        }
    }
    private void yarnTopPathGenerate()
    {
        pivotList.Clear();
        if (path == null || path.Count == 0) return;

        foreach (var waypoint in path)
        {
            if (waypoint == null) continue;
            pivotList.Add(waypoint.position - new Vector3(0, 0, waypoint.position.z + 1));
        }
    }
    IEnumerator dissolveTrigger()
    {
        float dissolveThreshold_value_x = 1f;
        float dissolveThreshold_value_y = 1f;
        dissolveThreshold_value_y = 1f;
        for (int i = 0; i < 6; i++)
        {
            dissolveThreshold_value_x = 1f;
            dissolveThreshold_value_y -= 0.19f;
            this_material.SetFloat("_DissolveThreshold_y", dissolveThreshold_value_y);
            for (int j = 0; j < 5; j++)
            {
                dissolveThreshold_value_x -= 0.2f;
                this_material.SetFloat("_DissolveThreshold_x", dissolveThreshold_value_x);
                yield return new WaitForSeconds(0.013f);
            }
        }
        Finish();
    }
    public void Finish()
    {
        if (blacknessTweener != null && blacknessTweener.IsActive())
        {
            blacknessTweener.Kill();
        }
        yarnball.fall();
        Destroy(transform.gameObject);
    }
    private int CheckOcclusionCount(int layer=-1) 
    {
        var colliderA = transform.GetComponent<PolygonCollider2D>();
        if (layer < 0)
        {
            occlusionCount = 0;
            List<PolygonCollider2D> colList = new List<PolygonCollider2D>();
            for (int i = 0; i < clothPool.childCount; i++)
            {
                var otherRoot = clothPool.GetChild(i);
                if (otherRoot == transform || otherRoot.position.z - transform.position.z > 3)
                {
                    continue;
                }
                otherRoot.GetComponent<PolygonCollider2D>().enabled = true;
                colList.Add(otherRoot.GetComponent<PolygonCollider2D>());
            }//add polygon
            List<float> layers = new List<float>();
            for (int i = 0; i < colList.Count; i++)
            {
                var colliderB = colList[i];
                if (colliderA.IsTouching(colliderB)
                    && colliderA.transform.position.z > colliderB.transform.position.z)
                {
                    var occlusionCountB = colliderB.transform.GetComponent<clothController>().occlusionCount;
                    occlusionCount = Mathf.Max(occlusionCount, occlusionCountB + 1);
                    if (occlusionCount >= 2) break;
                }
            }
        }
        else
        {
            occlusionCount = layer;
        }
        return occlusionCount;
    }
    public void UpdateBlackness(int layer = -1)
    {
        if (this_material == null)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr?.material == null) return;
            this_material = sr.material;
        }
        int occlusionCount = CheckOcclusionCount(layer);
        if (occlusionCount>0)
        {
            transform.GetChild(1).gameObject.SetActive(true);
        }
        float targetBlackness = occlusionCount == 0 ? 1f
            : (occlusionCount == 1 ? 0.4f : 0.1f);
       
        if (blacknessTweener != null && blacknessTweener.IsActive())
        {
            blacknessTweener.Kill();
        }
        
        blacknessTweener = DOTween.To(
            () => this_material.GetFloat("_Blackness"),
            x => this_material.SetFloat("_Blackness", x),
            targetBlackness,
            0.1f
        ).SetEase(Ease.Linear);
    }
    /// <summary>
    /// 返回是否能被点击收集
    /// </summary>
    /// <returns></returns>
    public bool colliderCheck()
    {
        int occlusionCount = CheckOcclusionCount();
        Debug.Log(occlusionCount);
        return occlusionCount == 0;
    }
    public (ropeController, yarnBallController) ropeGenerate()
    {
        var rope = Instantiate(GameManager.instance.ropeGroup
            , new Vector3(transform.position.x, transform.position.y - 2, -1)
            , Quaternion.identity
            , transform);
        rope.transform.GetChild(1).GetComponent<yarnBallController>().color = color;
        return (rope.transform.GetChild(0).GetComponent<ropeController>()
               , rope.transform.GetChild(1).GetComponent<yarnBallController>());
    }
}
