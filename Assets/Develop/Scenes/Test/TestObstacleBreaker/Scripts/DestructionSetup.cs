using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DestructionSetup : MonoBehaviour
{
    [Header("破壊前モデル")]
    [SerializeField]
    private GameObject boxBefore;

    [Header("破壊後モデル")]
    [SerializeField]
    private GameObject boxAfter;

    [Header("破片の密度")]
    [SerializeField]
    private float density = 200.0f;

    [Header("爆発設定")]
    [SerializeField]
    private float explosionForce = 5.0f;

    [SerializeField]
    private float explosionRadius = 3.0f;

    [Header("爆発位置")]
    [SerializeField]
    private Transform explosionPoint;


    // 破片の初期Transformを保存するクラス
    private class PieceInitialState
    {
        public Rigidbody rigidbody;

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
    }


    private readonly List<PieceInitialState> initialStates = new();


    private void Start()
    {
        // 破壊後モデルのセットアップ
        SetupComponents(boxAfter);

        // 破片の質量を密度から設定
        SetDensityAll(boxAfter);

        // 破片の初期状態を保存
        SaveInitialStates();

        // 初期状態を破壊前に設定
        ResetDestruction();
    }


    private void Update()
    {
        // Spaceキーで破壊
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Break();
        }

        // Rキーで破壊前に戻す
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetDestruction();
        }
    }


    /// <summary>
    /// 破壊モデルの各破片に
    /// RigidbodyとMeshColliderを追加する
    /// </summary>
    private void SetupComponents(GameObject root)
    {
        MeshRenderer[] renderers =
            root.GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer renderer in renderers)
        {
            GameObject piece = renderer.gameObject;

            // Rigidbodyを取得
            Rigidbody rb = piece.GetComponent<Rigidbody>();

            // Rigidbodyがなければ追加
            if (rb == null)
            {
                rb = piece.AddComponent<Rigidbody>();
            }

            // 破壊するまで物理演算を無効
            rb.isKinematic = true;

            // MeshColliderを取得
            MeshCollider meshCollider =
                piece.GetComponent<MeshCollider>();

            // MeshColliderがなければ追加
            if (meshCollider == null)
            {
                meshCollider = piece.AddComponent<MeshCollider>();
            }

            // Rigidbody付きMeshColliderなのでConvexを有効化
            meshCollider.convex = true;
        }
    }


    /// <summary>
    /// 全破片の質量を密度から設定する
    /// </summary>
    private void SetDensityAll(GameObject root)
    {
        Rigidbody[] rigidbodies =
            root.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.SetDensity(density);
        }
    }


    /// <summary>
    /// 破壊前の破片のTransformを保存する
    /// </summary>
    private void SaveInitialStates()
    {
        initialStates.Clear();

        Rigidbody[] rigidbodies =
            boxAfter.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody rb in rigidbodies)
        {
            Transform pieceTransform = rb.transform;

            PieceInitialState state = new PieceInitialState
            {
                rigidbody = rb,
                position = pieceTransform.position,
                rotation = pieceTransform.rotation,
                localScale = pieceTransform.localScale
            };

            initialStates.Add(state);
        }
    }


    /// <summary>
    /// 木箱を破壊する
    /// </summary>
    private void Break()
    {
        // すでに破壊済みなら何もしない
        if (!boxBefore.activeSelf)
        {
            return;
        }

        // 破壊前モデルを非表示
        boxBefore.SetActive(false);

        // 破壊後モデルを表示
        boxAfter.SetActive(true);

        // 爆発位置を決定
        Vector3 explosionPosition;

        if (explosionPoint != null)
        {
            explosionPosition = explosionPoint.position;
        }
        else
        {
            explosionPosition = transform.position;
        }

        // 全破片のRigidbodyを取得
        Rigidbody[] rigidbodies =
            boxAfter.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody rb in rigidbodies)
        {
            // 物理演算を有効化
            rb.isKinematic = false;

            // 爆発力を加える
            rb.AddExplosionForce(
                explosionForce,
                explosionPosition,
                explosionRadius
            );
        }
    }


    /// <summary>
    /// 破壊前の状態に戻す
    /// </summary>
    private void ResetDestruction()
    {
        // 破壊前モデルを表示
        boxBefore.SetActive(true);

        // 破壊後モデルを表示状態にする
        boxAfter.SetActive(true);

        // 各破片を初期位置に戻す
        foreach (PieceInitialState state in initialStates)
        {
            Rigidbody rb = state.rigidbody;
            Transform pieceTransform = rb.transform;

            // 物理演算を無効化
            rb.isKinematic = true;

            // 速度をリセット
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Transformを初期状態に戻す
            pieceTransform.position = state.position;
            pieceTransform.rotation = state.rotation;
            pieceTransform.localScale = state.localScale;
        }

        // 破壊後モデルを非表示
        boxAfter.SetActive(false);
    }
}