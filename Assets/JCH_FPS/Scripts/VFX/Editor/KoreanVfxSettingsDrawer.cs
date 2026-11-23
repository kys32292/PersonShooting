using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PesonShooting.VFX.Editor
{
    // 2026-11-23: 기본 Inspector가 필드의 InspectorName을 반영하지 않아도 한국어 이름과 설명을 직접 표시합니다.
    // SerializedProperty를 그대로 편집하므로 저장 필드명, Undo, 프리팹/에셋 값과 기존 범위 속성을 유지합니다.
    public abstract class KoreanVfxSettingsDrawer : PropertyDrawer
    {
        protected abstract Type SettingsType { get; }
        protected virtual string HelpText => null;

        // 2026-11-23: 접힌 상태와 펼친 상태의 높이를 실제 필드 높이에 맞춰 계산해 아래 설정과 겹치지 않게 합니다.
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded) return height;

            if (!string.IsNullOrEmpty(HelpText))
                height += EditorGUIUtility.standardVerticalSpacing + GetHelpHeight();

            SerializedProperty child = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            if (child.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(child, end) && child.depth == property.depth + 1)
                {
                    height += EditorGUIUtility.standardVerticalSpacing
                        + EditorGUI.GetPropertyHeight(child, GetChildLabel(child), true);
                    if (!child.NextVisible(false)) break;
                }
            }
            return height;
        }

        // 2026-11-23: 현재 설정의 바로 아래 필드만 그려 같은 drawer를 재귀 호출하지 않습니다.
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = GetGroupLabel(label);
            EditorGUI.BeginProperty(position, label, property);
            int previousIndent = EditorGUI.indentLevel;
            try
            {
                var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);
                if (!property.isExpanded) return;
                EditorGUI.indentLevel++;

                if (!string.IsNullOrEmpty(HelpText))
                {
                    row.y = row.yMax + EditorGUIUtility.standardVerticalSpacing;
                    row.height = GetHelpHeight();
                    EditorGUI.HelpBox(EditorGUI.IndentedRect(row), HelpText, MessageType.Info);
                }

                SerializedProperty child = property.Copy();
                SerializedProperty end = property.GetEndProperty();
                if (!child.NextVisible(true)) return;
                while (!SerializedProperty.EqualContents(child, end) && child.depth == property.depth + 1)
                {
                    GUIContent childLabel = GetChildLabel(child);
                    row.y = row.yMax + EditorGUIUtility.standardVerticalSpacing;
                    row.height = EditorGUI.GetPropertyHeight(child, childLabel, true);
                    EditorGUI.PropertyField(row, child, childLabel, true);
                    if (!child.NextVisible(false)) break;
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndent;
                EditorGUI.EndProperty();
            }
        }

        // 2026-11-23: 배열 요소는 Element 번호를 유지하고 설정 그룹에만 루트 필드의 한국어 표시명을 적용합니다.
        private GUIContent GetGroupLabel(GUIContent fallback)
        {
            return fieldInfo != null && fieldInfo.FieldType == SettingsType
                ? GetFieldLabel(fieldInfo, fallback)
                : fallback;
        }

        // 2026-11-23: 각 필드에 이미 작성된 한국어 표시명과 툴팁을 재사용해 설명을 중복 저장하지 않습니다.
        private GUIContent GetChildLabel(SerializedProperty child)
        {
            FieldInfo field = SettingsType.GetField(child.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return GetFieldLabel(field, new GUIContent(child.displayName, child.tooltip));
        }

        // 2026-11-23: 이름/툴팁 속성이 없는 필드는 Unity 기본 표시를 유지하여 기존 Inspector와 호환합니다.
        private static GUIContent GetFieldLabel(FieldInfo field, GUIContent fallback)
        {
            if (field == null) return fallback;
            var name = field.GetCustomAttribute<InspectorNameAttribute>();
            var tooltip = field.GetCustomAttribute<TooltipAttribute>();
            return new GUIContent(name != null ? name.displayName : fallback.text,
                tooltip != null ? tooltip.tooltip : fallback.tooltip);
        }

        // 2026-11-23: 좁은 Inspector에서는 안내문이 줄바꿈되도록 가용 폭으로 높이를 계산합니다.
        private float GetHelpHeight()
        {
            float width = Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 70f);
            return EditorStyles.helpBox.CalcHeight(new GUIContent(HelpText), width);
        }
    }

    // 2026-11-23: 설정 타입별 등록을 분리해 Unity 버전별 중복 CustomPropertyDrawer 속성 처리에 의존하지 않습니다.
    [CustomPropertyDrawer(typeof(VfxConfig.Entry))]
    public sealed class KoreanVfxEntryDrawer : KoreanVfxSettingsDrawer
    {
        protected override Type SettingsType => typeof(VfxConfig.Entry);
    }

    [CustomPropertyDrawer(typeof(VfxConfig.BulletTrailSettings))]
    public sealed class KoreanBulletTrailDrawer : KoreanVfxSettingsDrawer
    {
        protected override Type SettingsType => typeof(VfxConfig.BulletTrailSettings);
    }

    [CustomPropertyDrawer(typeof(VfxConfig.PerkAbsorbSettings))]
    public sealed class KoreanPerkAbsorbDrawer : KoreanVfxSettingsDrawer
    {
        protected override Type SettingsType => typeof(VfxConfig.PerkAbsorbSettings);
    }

    [CustomPropertyDrawer(typeof(VfxConfig.GravityWellSettings))]
    public sealed class KoreanGravityWellDrawer : KoreanVfxSettingsDrawer
    {
        protected override Type SettingsType => typeof(VfxConfig.GravityWellSettings);
    }

    [CustomPropertyDrawer(typeof(VfxConfig.GravityProjectileSettings))]
    public sealed class KoreanGravityProjectileDrawer : KoreanVfxSettingsDrawer
    {
        protected override Type SettingsType => typeof(VfxConfig.GravityProjectileSettings);
    }

    [CustomPropertyDrawer(typeof(VfxConfig.ExplosiveAmmoSettings))]
    public sealed class KoreanExplosiveAmmoDrawer : KoreanVfxSettingsDrawer
    {
        protected override Type SettingsType => typeof(VfxConfig.ExplosiveAmmoSettings);
        protected override string HelpText => "크기는 '폭발 외형 크기 배율'로 조절합니다. 비행 길이와 굵기는 '일반 총알 궤적' 설정을 공유합니다. 항목에 마우스를 올리면 단위와 역할이 표시됩니다. 수정 후 테스트 씬에서 다시 재생하세요.";
    }

    // 2026-11-23: 관통탄도 모든 조절 항목을 한국어 이름/설명으로 표시합니다.
    [CustomPropertyDrawer(typeof(VfxConfig.PiercingAmmoSettings))]
    public sealed class KoreanPiercingAmmoDrawer : KoreanVfxSettingsDrawer
    {
        protected override Type SettingsType => typeof(VfxConfig.PiercingAmmoSettings);
        protected override string HelpText => "관통탄 자체의 금빛 광선입니다. 광선 길이/중심 굵기/탄두 별 크기로 외형을 조절합니다. 실제 충돌 반경과 관통 횟수는 관통탄 퍽 설정입니다. 항목에 마우스를 올리면 한국어 설명이 표시됩니다.";
    }

    // 2026-11-23: 폭발 적 설정도 한국어 조절 항목과 실제 피해 반경의 차이를 안내합니다.
    [CustomPropertyDrawer(typeof(VfxConfig.BomberExplosionSettings))]
    public sealed class KoreanBomberExplosionDrawer : KoreanVfxSettingsDrawer
    {
        protected override Type SettingsType => typeof(VfxConfig.BomberExplosionSettings);
        protected override string HelpText => "폭발탄과 같은 화염 질감에 노란빛을 높인 구형 폭발입니다. 크기 배율/중심색/주요 노란색/가장자리색을 조절하세요. 실제 피해 반경은 폭발 적 프리팹의 폭발 범위 설정입니다.";
    }

    // 2026-11-23: 플레이어 피격 효과도 한국어 수치 설명을 공통 Inspector에 표시합니다.
    [CustomPropertyDrawer(typeof(VfxConfig.PlayerHitSettings))]
    public sealed class KoreanPlayerHitDrawer : KoreanVfxSettingsDrawer
    {
        protected override Type SettingsType => typeof(VfxConfig.PlayerHitSettings);
        protected override string HelpText => "피격 시 약하게 흔들고, 실제 HP 50 이하에서 옅은 피 테두리를 표시합니다. 흔들림 세기/시간과 시작 체력/불투명도/테두리 폭을 조절합니다. 회복 시 사라지며 중앙 시야와 UI 입력을 가리지 않습니다.";
    }

    [CustomPropertyDrawer(typeof(VfxConfig.GrenadeAmmoSettings))]
    public sealed class KoreanGrenadeAmmoDrawer : KoreanVfxSettingsDrawer
    {
        protected override Type SettingsType => typeof(VfxConfig.GrenadeAmmoSettings);
        protected override string HelpText => "전체 크기는 '폭발 외형 크기 배율', 화염 높이는 '화염 기둥 세로 배율'로 조절합니다. 연기와 먼지는 별도 항목입니다. 항목에 마우스를 올리면 단위와 역할을 볼 수 있습니다.";
    }
}
