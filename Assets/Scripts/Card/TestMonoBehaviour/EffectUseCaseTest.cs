using System.Collections.Generic;
using UnityEngine;

public class EffectUseCaseTest : MonoBehaviour
{
    [SerializeField]
    PoolDataBase _poolDataBase;
    [SerializeField]
    EffectDataBase _effectDataBase;
    [SerializeField]
    CardDataBase _cardDataBase;

    [SerializeField]
    int _poolID;
    [SerializeField]
    int upCardID;
    [SerializeField]
    int downCardID;
    [SerializeField]
    int leftCardID;
    [SerializeField]
    int rightCardID;

    EffectSystem _effectSystem;
    CardSystem _cardSystem;
    PoolSystem _poolSystem;
    EffectUseCase _effectUseCase;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _cardDataBase.CheckAllData();
        Dictionary<SlotDirection, int> haveCardID = new Dictionary<SlotDirection, int>();
        haveCardID[SlotDirection.Up] = upCardID;
        haveCardID[SlotDirection.Down] = downCardID;
        haveCardID[SlotDirection.Left] = leftCardID;
        haveCardID[SlotDirection.Right] = rightCardID;

        _cardSystem = new CardSystem(_cardDataBase, haveCardID);
        _poolSystem = new PoolSystem(_poolID, _cardDataBase.FallBackCard.CardID, _poolDataBase);
        _effectSystem = new EffectSystem(_effectDataBase);

        _effectUseCase = new EffectUseCase(_effectSystem, _poolSystem, _cardSystem);

        RegisterSlotCardCommand upCardCommand = new RegisterSlotCardCommand(SlotDirection.Up);
        RegisterSlotCardCommand rightCardCommand = new RegisterSlotCardCommand(SlotDirection.Right);
        ApplyEffectCommand applyCommand = new ApplyEffectCommand();
        AdvanceTurnCommand advanceTurnCommand = new AdvanceTurnCommand(1);
        Dictionary<SlotDirection, int> slot = new Dictionary<SlotDirection, int>();
        slot.Add(SlotDirection.Up, 4);
        SetSlotCardCommand setSlotCardCommand = new SetSlotCardCommand(slot);
        _effectUseCase.RegisterUseCard(upCardCommand);
        _effectUseCase.RegisterUseCard(upCardCommand);
        _effectUseCase.RegisterUseCard(rightCardCommand);

        _effectUseCase.ApplyEffect(applyCommand);
        _effectUseCase.AdvanceTurn(advanceTurnCommand);

        _effectUseCase.SetSlotCard(setSlotCardCommand);
        _effectUseCase.RegisterUseCard(upCardCommand);
        _effectUseCase.RegisterUseCard(upCardCommand);
        _effectUseCase.RegisterUseCard(rightCardCommand);

        _effectUseCase.ApplyEffect(applyCommand);
        _effectUseCase.AdvanceTurn(advanceTurnCommand);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
