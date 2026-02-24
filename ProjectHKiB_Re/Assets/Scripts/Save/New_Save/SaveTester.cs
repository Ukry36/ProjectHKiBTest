using UnityEngine;

public class SaveTester : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private SaveModule saveModule;
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private GearManager gearManager;

    [Header("Slot")]
    [SerializeField] private int slot = 0;

    [Header("Optional")]
    [SerializeField] private Component playerRoot;
    [SerializeField] private MonoBehaviour eventProviderBehaviour;

    [Header("Hotkeys (Optional)")]
    [SerializeField] private bool useHotkeys = true;
    [SerializeField] private KeyCode saveKey = KeyCode.F5;
    [SerializeField] private KeyCode loadKey = KeyCode.F9;

    private IEventSaveProvider EventProvider => eventProviderBehaviour as IEventSaveProvider;

    private void Reset()
    {
        // 자동 참조(있으면 잡힘)
        if (saveModule == null) saveModule = FindFirstObjectByType<SaveModule>();
        if (inventory == null) inventory = FindFirstObjectByType<InventoryManager>();
        if (gearManager == null) gearManager = FindFirstObjectByType<GearManager>();
    }

    private void Update()
    {
        if (!useHotkeys) return;

        if (Input.GetKeyDown(saveKey))
            Save();

        if (Input.GetKeyDown(loadKey))
            Load();
    }

    public void Save()
    {
        if (!ValidateRefs()) return;
        saveModule.StartSave(slot, inventory, gearManager, playerRoot, EventProvider);
    }

    public void Load()
    {
        if (!ValidateRefs()) return;
        saveModule.StartLoad(slot, inventory, gearManager, playerRoot, EventProvider);
    }

    public void SetSlot(int newSlot)
    {
        slot = Mathf.Max(0, newSlot);
    }

    private bool ValidateRefs()
    {
        if (saveModule == null)
        {
            Debug.LogError("[SaveTester] SaveModule reference is missing.");
            return false;
        }
        if (inventory == null)
        {
            Debug.LogError("[SaveTester] InventoryManager reference is missing.");
            return false;
        }
        if (gearManager == null)
        {
            Debug.LogError("[SaveTester] GearManager reference is missing.");
            return false;
        }
        return true;
    }
}