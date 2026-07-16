using Animal.Data;
using System;
using System.Collections.Generic;
using TaskTown.Gacha;
using UnityEngine;


/// <summary>
/// AnimalDatabase의 등록 상태를 Inspector에서 확인하기 위한 검증용 스크립트입니다.
/// 실제 데이터 원본은 AnimalDatabase SO이며,
/// 이 컴포넌트의 리스트는 데이터 등록 확인을 위한 복사본입니다.
/// </summary>
public class AnimalDatabaseList : MonoBehaviour
{

    [Header("동물 데이터베이스")]
    [Tooltip("실제 데이터 원본")]
    [SerializeField] private AnimalDatabase animalDatabase;

    [Header("전체 동물 리스트")]
    [Tooltip("AnimalDatabase의 등록 상태를 Inspector에서 확인하기 위한 검증용 리스트\n게임에서 사용되는 모든 동물 데이터를 저장합니다.")]
    [SerializeField] private List<AnimalDataSO> allAnimalList = new();

    
    [Header("등급별 동물 리스트\n각 등급에 어떤 동물이 포함되어 있는지 확인합니다")]
    [SerializeField] private List<AnimalDataSO> gradeAnimal_Normal = new List<AnimalDataSO>();
    [SerializeField] private List<AnimalDataSO> gradeAnimal_Rare = new List<AnimalDataSO>();
    [SerializeField] private List<AnimalDataSO> gradeAnimal_Epic = new List<AnimalDataSO>();   
    [SerializeField] private List<AnimalDataSO> gradeAnimal_Unique = new List<AnimalDataSO>();
    [SerializeField] private List<AnimalDataSO> gradeAnimal_Legendary = new List<AnimalDataSO>();

    public IReadOnlyList<AnimalDataSO> AllAnimalList => allAnimalList;
    public IReadOnlyList<AnimalDataSO> GradeAnimalNormal => gradeAnimal_Normal;
    public IReadOnlyList<AnimalDataSO> GradeAnimalRare => gradeAnimal_Rare;
    public IReadOnlyList<AnimalDataSO> GradeAnimalEpic => gradeAnimal_Epic;
    public IReadOnlyList<AnimalDataSO> GradeAnimalUnique => gradeAnimal_Unique;
    public IReadOnlyList<AnimalDataSO> GradeAnimalLegendary => gradeAnimal_Legendary;

    private void Awake()
    {
        RefreshDatabaseList();
    }


    /// <summary>
    /// AnimalDatabase의 전체 목록을 가져와 등급별 리스트로 분류합니다.
    /// Inspector의 컴포넌트 메뉴에서도 실행할 수 있습니다.
    /// </summary>
    [ContextMenu("동물 데이터베이스 목록 갱신")]
    private void RefreshDatabaseList()
    {
        ClearLists();

        if (animalDatabase == null)
        {
            Debug.LogWarning("[AnimalDatabaseList] AnimalDatabase 가 연결되지 않았습니다.", this);
            return;
        }

        IReadOnlyList<AnimalDataSO> databaseAnimals = animalDatabase.Animals;

        if(databaseAnimals == null)
        {
            Debug.LogWarning("[AnimalDatabaseList] AnimalDatabase 의 동물 목록이 없습니다.", this);
            return;
        }

        foreach (AnimalDataSO animal in databaseAnimals)
        {
            if (animal == null)
                continue;

            allAnimalList.Add(animal);
            AddAnimalByGrade(animal);

        }

        SortListsByDexIndex();

        Debug.Log(
                $"[AnimalDatabaseList] 목록 갱신 완료 / " +
                $"전체: {allAnimalList.Count}, " +
                $"Normal: {gradeAnimal_Normal.Count}, " +
                $"Rare: {gradeAnimal_Rare.Count}, " +
                $"Epic: {gradeAnimal_Epic.Count}, " +
                $"Unique: {gradeAnimal_Unique.Count}, " +
                $"Legend: {gradeAnimal_Legendary.Count}",
                this
            );
    }

    /// <summary>
    /// 등급별 동물 리스트에 추가
    /// </summary>
    private void AddAnimalByGrade(AnimalDataSO animal)
    {
        switch (animal.Grade)
        {
            case ItemGrade.Normal:
                gradeAnimal_Normal.Add(animal);
                break;

            case ItemGrade.Rare:
                gradeAnimal_Rare.Add(animal);
                break;

            case ItemGrade.Epic:
                gradeAnimal_Epic.Add(animal);
                break;

            case ItemGrade.Unique:
                gradeAnimal_Unique.Add(animal);
                break;

            case ItemGrade.Legendary:
                gradeAnimal_Legendary.Add(animal);
                break;

            default:
                Debug.LogWarning(
                    $"[AnimalDatabaseList] 분류되지 않은 등급입니다. " +
                    $"ID: {animal.Id}, Grade: {animal.Grade}",
                    animal
                );
                break;
        }
    }

    /// <summary>
    /// 동물을 인덱스로 정렬
    /// </summary>
    private void SortListsByDexIndex()
    {
        allAnimalList.Sort(CompareByDexIndex);
        gradeAnimal_Normal.Sort(CompareByDexIndex);
        gradeAnimal_Rare.Sort(CompareByDexIndex);
        gradeAnimal_Epic.Sort(CompareByDexIndex);
        gradeAnimal_Unique.Sort(CompareByDexIndex);
        gradeAnimal_Legendary.Sort(CompareByDexIndex);
    }

    /// <summary>
    /// 인덱스 정렬
    /// </summary>
    private int CompareByDexIndex(AnimalDataSO left, AnimalDataSO right)
    {
        if (left == null && right == null)
            return 0;

        if (left == null)
            return 1;

        if (right == null)
            return -1;

        return left.DexIndex.CompareTo(right.DexIndex);
    }


    /// <summary>
    /// 리스트 초기화
    /// </summary>
    private void ClearLists()
    {
        allAnimalList.Clear();
        gradeAnimal_Normal.Clear();
        gradeAnimal_Rare.Clear();
        gradeAnimal_Epic.Clear();
        gradeAnimal_Unique.Clear();
        gradeAnimal_Legendary.Clear();
    }

}
