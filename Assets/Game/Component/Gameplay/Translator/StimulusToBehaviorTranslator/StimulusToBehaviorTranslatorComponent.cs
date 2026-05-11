// Deprecated: replaced by MoveStimulusTranslator + FireStimulusTranslator.
// Keep this file so existing Unity asset references remain valid until prefabs are migrated.

public sealed partial class StimulusToBehaviorTranslatorComponentData
{
    public string Key;
}

public sealed partial class StimulusToBehaviorTranslatorComponent
{
    private readonly StimulusToBehaviorTranslatorComponentData _componentData;
}
