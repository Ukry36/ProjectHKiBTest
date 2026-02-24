using System;
using System.Collections.Generic;

[Serializable]
public class ItemSaveInfo
{
    public string itemGuid;
    public int count;
}

[Serializable]
public class GearSaveInfo
{
    public string gearGuid;
    public int slot;
    public List<int> equippedCards;
}

[Serializable]
public class CardSaveInfo
{
    public string cardName;
    public List<string> gearGuids; // 슬롯별
}

[Serializable]
public class EventFlagSaveInfo
{
    public string id;
    public bool value;
}

[Serializable]
public class PassageSaveInfo
{
    public string id;
    public bool opened;
}

[Serializable]
public class SaveSlotData
{
    public string savedAt;

    public float hp;
    
    public List<ItemSaveInfo> items = new();
    public List<GearSaveInfo> ownedGears = new();
    public List<CardSaveInfo> cards = new();

    public List<EventFlagSaveInfo> eventFlags = new();
    public List<PassageSaveInfo> passages = new();
}
