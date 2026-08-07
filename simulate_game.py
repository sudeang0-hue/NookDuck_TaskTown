import random
import statistics

# ---------------------------------------------------------------
# 동물 종류/등급 수/확률은 첨부 엑셀 '동물' 시트 실제값입니다.
# 등급별 생산량은 밸런스 조정(#2: 등급 격차 축소)으로 재설계했습니다.
# 원본(1/3/10/30/100, Normal-Legendary 100배 격차) -> 1차 조정(1/2/4/8/16, 16배 격차) ->
# Legendary가 Unique보다 뽑기 1회당 기대가치/등급 전체 기여도가 낮은 역전 현상이 발견되어
# Legendary만 40으로 재상향(40배 격차) ->
# #11 리밸런스: Unique->Legendary만 5배(8->40)로 유독 크게 뛰는 문제(#3) 발견. 등급확률
# 비율(Unique 8%/Legendary 2%=4배) 때문에 기여도 역전을 안 만들려면 Legendary가 Unique의
# 최소 4배(32)는 필요하다는 걸 확인(Unique를 올려도 이 배율 자체는 줄지 않음), 그래서
# 최소치보다 살짝 여유만 둔 34(짝수, 4.25배)로 낮춤 - EV/기여도 단조증가는 그대로 유지됨
# 2026.08.05: 개별 레벨업 기준비용을 등급 공통 단일값(100)에서 등급별 기본생산량에
# 비례하는 값으로 교체(GachaEntryData.levelUpBaseCost, 사용자 확인). Normal 20000
# 기준값 x prod 배수 그대로 비례(1/2/4/8/34배 -> 20000/40000/80000/160000/680000).
GRADES = [
    {"name": "Normal",    "count": 12, "prob": 0.45, "prod": 1,  "levelup_base_cost": 20_000},
    {"name": "Rare",      "count": 10, "prob": 0.30, "prod": 2,  "levelup_base_cost": 40_000},
    {"name": "Epic",      "count": 8,  "prob": 0.15, "prod": 4,  "levelup_base_cost": 80_000},
    {"name": "Unique",    "count": 6,  "prob": 0.08, "prod": 8,  "levelup_base_cost": 160_000},
    {"name": "Legendary", "count": 4,  "prob": 0.02, "prod": 34, "levelup_base_cost": 680_000},
]
TOTAL_COUNT = sum(g["count"] for g in GRADES)  # 40

# 대부분의 동물/도구는 처음엔 잠겨 있다가 마을 업그레이드를 하면 해금되는 방식으로 설계했습니다.
# (기획 엑셀 '마을' 시트의 '해금 동물'/'해금 도구' 컬럼이 비어있어 제안한 값)
# 마을 레벨 1부터 Normal+Rare(흔한 22종)는 전부 미리 풀어두고, 나머지(Epic/Unique/Legendary,
# 18종)는 레벨마다 조금씩 풀립니다.
#
# #18 리밸런스(도구 상한 기능 추가): 예전엔 "등급 블록 순서대로(Epic 8종 다 풀고 -> Unique ->
# Legendary) + 곡선 파워로 속도만 조절"하는 방식이었는데, 도구 상한(아래 TOOL_CAPACITY_*)이
# 추가되면서 문제가 생겼습니다. 레벨 3~6 구간엔 상한은 계속 늘어나는데 채울 수 있는 건 여전히
# Normal/Rare뿐이라 생산 증가율 자체가 둔화되고, 7~9 구간에 Epic/Unique/Legendary가 몰려서
# 풀리며 생산이 급증 - 이 "생산 증가율의 굴곡"은 곡선 파워를 아무리 조정해도(1.6->1.0->0.7)
# 없어지지 않았습니다(N=150~300 스윕으로 확인). 그래서 등급을 순서대로 다 풀지 않고 섞어서
# 해금하는 방식(인터리빙)으로 교체 - 중간 레벨에도 Unique/Legendary를 조금씩 흘려보내서 생산
# 증가율 자체를 완만하게 만듭니다. 마을레벨 1~9별 누적 해금 개수(Epic/Unique/Legendary):
UNLOCK_SCHEDULE = {
    1: (0, 0, 0),
    2: (2, 0, 0),
    3: (4, 1, 0),
    4: (6, 2, 0),
    5: (8, 3, 1),
    6: (8, 4, 1),
    7: (8, 5, 2),
    8: (8, 6, 3),
    9: (8, 6, 4),
}
_UNLOCK_GRADE_ORDER = ["Normal", "Rare", "Epic", "Unique", "Legendary"]
_GRADE_COUNT = {g["name"]: g["count"] for g in GRADES}
_GRADE_PROB = {g["name"]: g["prob"] for g in GRADES}
_GRADE_PROD = {g["name"]: g["prod"] for g in GRADES}
_GRADE_LEVELUP_BASE_COST = {g["name"]: g["levelup_base_cost"] for g in GRADES}

# 2026.08.06: 사용자 요청으로 기존 일반 동물 8종을 시크릿 9종과 별개로 난이도 전용화
# (노말2/레어1/에픽1 x Hard,VeryHard). 등급 내 개체 인덱스(0-based, build_roster의
# 생성 순서와 동일) 기준으로 태그합니다. 노말 난이도 최대 수집 가능 개체수는
# 40 -> 32종(80%)로 줄어듭니다(사용자 확인).
GENERAL_DIFFICULTY_EXCLUSIVE = {
    ("Normal", 0): "Hard",      # 늑대
    ("Normal", 1): "Hard",      # 곰
    ("Normal", 2): "VeryHard",  # 여우
    ("Normal", 3): "VeryHard",  # 사슴
    ("Rare", 0): "Hard",        # 표범
    ("Rare", 1): "VeryHard",    # 독수리
    ("Epic", 0): "Hard",        # 버팔로
    ("Epic", 1): "VeryHard",    # 코끼리
}

# 2026.08.05 설계: 시크릿 동물 9종(Hard 4 + VeryHard 5). 등급 누적 해금 스케줄과 무관하게
# 자체 unlockTownLevel + 난이도 일치 여부로만 해금됩니다. 등급별 확률/생산량/개별 레벨업
# 비용은 소속 등급(Unique/Legendary) 값을 그대로 공유합니다(라이브 GachaPoolData 확인 완료).
SECRET_ANIMALS = [
    # (grade, unlockTownLevel, difficulty, 참고용 이름)
    ("Unique", 5, "Hard", "armadillo"),
    ("Unique", 6, "Hard", "Chick"),
    ("Legendary", 7, "Hard", "giraffe"),
    ("Legendary", 8, "Hard", "kangaroo"),
    ("Unique", 5, "VeryHard", "mammoth"),
    ("Unique", 6, "VeryHard", "otter"),
    ("Legendary", 7, "VeryHard", "rhino"),
    ("Legendary", 8, "VeryHard", "seal"),
    ("Legendary", 9, "VeryHard", "unicorn"),
]


def unlocked_pool(roster, town_level, difficulty):
    # 해당 town_level/difficulty 조합에서 실제로 "뽑기 풀에 존재"하는 개체만 반환합니다.
    # 일반 개체: 기존 등급 누적 해금 로직(카운터)은 원래 인덱스 순서 그대로 유지하고,
    # 난이도 전용 태그가 있는 개체만 difficulty 불일치 시 결과에서 제외합니다(다른 개체의
    # 해금 타이밍에 영향 없음). 시크릿 개체: 등급 카운터와 무관, 자체 unlockTownLevel +
    # 난이도 일치 여부로만 판정합니다.
    town_level = min(max(town_level, 1), 9)
    epic_n, uniq_n, leg_n = UNLOCK_SCHEDULE[town_level]
    limits = {
        "Normal": _GRADE_COUNT["Normal"],
        "Rare": _GRADE_COUNT["Rare"],
        "Epic": epic_n,
        "Unique": uniq_n,
        "Legendary": leg_n,
    }
    counters = {g: 0 for g in _UNLOCK_GRADE_ORDER}
    result = []
    for item in roster:
        if item.get("is_secret"):
            if town_level >= item["unlockTownLevel"] and item["difficulty"] == difficulty:
                result.append(item)
            continue
        g = item["grade"]
        if counters[g] < limits[g]:
            counters[g] += 1
            item_diff = item.get("difficulty")
            if item_diff is not None and item_diff != difficulty:
                continue
            result.append(item)
    return result

# #13: 도구 수치(등급 분포/생산량)는 동물과 1:1 미러링으로 확정 - 사용자 확인 완료.
# 도구 이름/설명 등 텍스트만 추후 추가 예정이며 밸런스 수치에는 영향 없음.
# 도구는 동물과 1:1 비율로 생성 (같은 등급 분포, 생산량은 동물과 동일하게 미러링).
# 시크릿 동물은 짝꿍 도구가 없습니다(실제 Tool 에셋 어디에도 시크릿 동물을 specialAnimalId로
# 지정한 곳이 없음 - 확인 완료). 도구 자체는 난이도 제한이 없어 모든 난이도에서 동일합니다.
def build_roster():
    animals, tools = [], []
    for g in GRADES:
        indiv_prob = g["prob"] / g["count"]
        for i in range(g["count"]):
            aid = f"A_{g['name']}_{i}"
            tid = f"T_{g['name']}_{i}"
            diff = GENERAL_DIFFICULTY_EXCLUSIVE.get((g["name"], i))
            animals.append({
                "id": aid, "grade": g["name"], "prod": g["prod"], "indiv_prob": indiv_prob,
                "levelup_base_cost": g["levelup_base_cost"], "difficulty": diff,
            })
            tools.append({
                "id": tid, "grade": g["name"], "prod": g["prod"], "indiv_prob": indiv_prob,
                "pair": aid, "levelup_base_cost": g["levelup_base_cost"],
            })

    for idx, (grade, unlock_lvl, diff, name) in enumerate(SECRET_ANIMALS):
        aid = f"S_{name}"
        animals.append({
            "id": aid, "grade": grade, "prod": _GRADE_PROD[grade],
            "indiv_prob": _GRADE_PROB[grade] / _GRADE_COUNT[grade],
            "levelup_base_cost": _GRADE_LEVELUP_BASE_COST[grade],
            "difficulty": diff, "is_secret": True, "unlockTownLevel": unlock_lvl,
        })

    return animals, tools

ANIMALS, TOOLS = build_roster()
TOOL_PAIR_OF_ANIMAL = {t["pair"]: t["id"] for t in TOOLS}
ANIMAL_BY_ID = {a["id"]: a for a in ANIMALS}
TOOL_BY_ID = {t["id"]: t for t in TOOLS}

# ---------------------------------------------------------------
# 게임 규칙 파라미터 (실제 코드 기본값 + 조정 대상 성장률)
# ---------------------------------------------------------------
# 2026.08.05: 사용자 요청으로 생산량 상승 배율을 1.2배(0.2)에서 2배(1.0)로 상향.
LEVEL_BONUS_RATE = 1.0          # AnimalData/ToolData - levelBonusRatePerLevel
SPECIAL_BONUS = 0.5             # ToolData.specialAnimalBonusRate
# #10 리밸런스: 4^레벨(4/16/64/256)은 코인 타이밍 문제(항상 0원)와 겹쳐 레벨업이 사실상
# 불가능했음. 환급/천장으로 코인 타이밍은 고쳤지만, 중복 요구치 자체도 2^레벨(2/4/8/16)로
# 낮춰서 레벨업 빈도와 희귀 등급 진행도를 함께 개선함 (사용자 확인 후 확정).
DUP_GROWTH = 2                   # LevelUpRequirementCalculator (2^레벨)
# LEVELUP_BASE_COST는 등급별 값으로 대체됨(위 GRADES의 levelup_base_cost 참고). 성장률만 공통.
LEVELUP_COST_RATE = 1.5

CLICK_TYPING_HOURLY_CAP = 10000 / 3600.0  # 코인/초 (EarnProcessor.cs maxCoinPerHour) - #17 이후 미사용(레거시 참고용)

# #16 리밸런스: 오프라인 보상. 오프라인 동안의 실제 생산량에 배율을 곱하고, 시간에 상한을
# 둬서 "보상 제한값"을 삼습니다(장기 리텐션 요소가 경제 밸런스를 깨지 않도록). 도감(수집
# 기록)은 난이도 리셋 후에도 유지하되 게임 수치에는 영향 없음 - 별도 밸런스 파라미터 없음.
OFFLINE_REWARD_MULTIPLIER = 0.025  # 오프라인 동안은 온라인 생산량의 2.5%만 인정
OFFLINE_REWARD_CAP_HOURS = 12       # 오프라인 보상으로 인정되는 최대 시간 (고정값)


def offline_reward_coins(production_rate_per_sec, offline_hours):
    """오프라인 시간 동안 지급할 보상 코인. 시간 상한 + 배율 감소를 함께 적용합니다."""
    capped_hours = min(max(offline_hours, 0.0), OFFLINE_REWARD_CAP_HOURS)
    return production_rate_per_sec * capped_hours * 3600.0 * OFFLINE_REWARD_MULTIPLIER

# 슬롯은 마을 레벨이 아니라 "새로운 종류를 처음 얻을 때" 늘어남 -> 슬롯 한도가
# 뽑기를 막는 일이 없으므로(처음 얻는 종류는 항상 바로 배치), 별도 capacity 값은 불필요.
# 대신 "언제까지 뽑고 언제부터 마을 업그레이드 비용을 모을지"는 두 선택지의
# 회수시간(payback = 비용 / 기대 생산량 증가분)을 비교해서 더 유리한 쪽에 코인을 쓰는
# "효율적 지출" 규칙으로 대체했습니다.

# #1: 뽑기 비용을 훨씬 가파르게 - 뽑기 자체가 진짜 소비처가 되도록 (깔끔한 값으로 확정)
# #14: 첫 뽑기 비용 1700->1500 (사용자 요청). 이 값이 레벨별 비용의 기준값이라
# 전체 레벨의 뽑기 비용이 같은 비율(약 11.8%)로 함께 낮아짐 - 검증 결과 완주시간
# 50.25h->48.74h(약 3% 단축), 단조성/레벨8~10 비중(49.8%)은 그대로 유지됨.
GACHA_BASE_COST = 1500
GACHA_COST_GROWTH = 1.5

# #15: 사용자 요청으로 뽑기 비용을 100단위로 깔끔하게 반올림 (공식값 대비 완주시간
# 48.74h->48.73h로 사실상 동일, 단조성/비중 그대로 유지 - 실제 사용되는 값은 이 표).
GACHA_COSTS = [1500, 2200, 3400, 5100, 7600, 11400, 17100, 25600, 38400]

# #3: 마을 업그레이드는 초반 비용을 올리고 성장률은 완만하게(뒤로 몰리지 않도록)
# 하나의 성장률 공식으로는 레벨1에 노말+레어를 몰아서 풀어주는 것과 "낮은 레벨이 높은
# 레벨보다 비중이 크면 안 된다"는 조건을 동시에 만족시킬 수 없어서(공식이 만드는 굴곡을
# 다른 파라미터로 상쇄하는 데 한계가 있었음), 실제 게임 코드(GachaCostConfig,
# TownUpgradeCostConfig)와 같은 방식으로 레벨 1~9 비용을 직접 나열해서 목표 구간 비중에
# 맞춰 하나하나 역산한 뒤, 깔끔한 숫자로 반올림하고 항상 증가하도록(내림 없이) 다듬었습니다.
TOWN_UPGRADE_COSTS = [
    77_000,       # 레벨1->2
    425_000,      # 레벨2->3
    1_220_000,    # 레벨3->4
    2_610_000,    # 레벨4->5
    7_820_000,    # 레벨5->6
    15_050_000,   # 레벨6->7
    21_450_000,   # 레벨7->8
    35_700_000,   # 레벨8->9
    45_200_000,   # 레벨9->10
]

# #4: 마을 업그레이드의 "생산 효율 버프" - 마을 레벨당 전체 생산량에 곱해지는 배율
# #17 리밸런스(이슈 #72): 이 자동 버프는 폐지되고, 아래 도구 효율 업그레이드로 대체됩니다.
EFFICIENCY_BUFF_PER_LEVEL = 0.1

# ---------------------------------------------------------------
# #17 리밸런스(이슈 #72): 클릭 코인/타이핑 코인/도구 효율 개별 업그레이드.
TOOL_EFF_BASE_COST = 60_000
TOOL_EFF_COST_GROWTH = 1.6
TOOL_EFF_BONUS_PER_LEVEL = EFFICIENCY_BUFF_PER_LEVEL  # 레벨당 +10% 생산 배율 (기존 자동 버프와 동일 폭)

# 2026.08.05: 사용자 요청으로 클릭/타이핑 구매 비용을 50배로 인상(300->15,000).
CLICK_UPGRADE_BASE_COST = 15_000
CLICK_UPGRADE_COST_GROWTH = 1.6
CLICK_CAP_BONUS_PER_LEVEL = 2000   # 코인/시간, 레벨당 시간당 상한 증가분

TYPING_UPGRADE_BASE_COST = 15_000
TYPING_UPGRADE_COST_GROWTH = 1.6
TYPING_CAP_BONUS_PER_LEVEL = 2000  # 코인/시간, 레벨당 시간당 상한 증가분

# 2026.08.05: 레벨10 완주 시점과 3종 업그레이드 만렙이 정확히 일치하도록 상한을 10->9로 낮춤.
UPGRADE_MAX_LEVEL_CAP = 9   # 절대 상한(일반 모드)
ENDLESS_MODE = False          # True면 아래 상한들이 전부 해제됨(엔드리스 모드용)


def upgrade_max_level(town_level):
    if ENDLESS_MODE:
        return float("inf")
    return min(town_level, UPGRADE_MAX_LEVEL_CAP)


# 실제 코드(SlotData_Tool.ToolLevelUp/SlotData_Animal 동일)는 개별 동물/도구 레벨을 5에서
# 막습니다.
ANIMAL_TOOL_MAX_LEVEL = 5

# ---------------------------------------------------------------
# #18(신규): 도구 상한(동시에 "동물이 장착된 도구" 슬롯 개수 제한).
TOOL_CAPACITY_BASE = 5
TOOL_CAPACITY_PER_LEVEL = 2  # 마을 레벨 1당 상한 증가분


def tool_capacity(town_level):
    return TOOL_CAPACITY_BASE + (town_level - 1) * TOOL_CAPACITY_PER_LEVEL


def tool_eff_cost(level):
    return TOOL_EFF_BASE_COST * (TOOL_EFF_COST_GROWTH ** level)


def click_upgrade_cost(level):
    return CLICK_UPGRADE_BASE_COST * (CLICK_UPGRADE_COST_GROWTH ** level)


def typing_upgrade_cost(level):
    return TYPING_UPGRADE_BASE_COST * (TYPING_UPGRADE_COST_GROWTH ** level)


# 2026.08.06: 레벨10(마을 상한) 도달 후 "재건(엔딩)" 선택 팝업을 열기 위해 지불하는 비용
# (VillageSystemManager.villageCompletionCost, ALL_UI_Connect_root.prefab 실제 반영값과 동일).
VILLAGE_COMPLETION_COST = 25_000_000


# ---------------------------------------------------------------
# #9 리밸런스: 죽은 재고(레벨업이 사실상 안 되는 문제) 개선 메커니즘 + 민감도 실험용 파라미터
# ---------------------------------------------------------------
DUP_REFUND_RATE = 0.3
PITY_ROLLS = 100
CLICK_UTILIZATION = 1.0
# #12 리밸런스: 레벨10 완주("엔딩") 시 코인/마을레벨/보유 동물·도구/도감을 전부 초기화하고
# 다음 난이도로 재도전하는 구조. DIFFICULTY_MULT_TABLE이 난이도별 생산 배율입니다.
# 2026.08.06: 사용자 요청으로 Easy 난이도 제거(Normal/Hard/VeryHard 3단계로 축소).
# 배율(DifficultyProductionTable_Default.asset과 동일): Normal=1.0/Hard=0.8/VeryHard=0.6.
DIFFICULTY_MULT_TABLE = {"Normal": 1.0, "Hard": 0.8, "VeryHard": 0.6}


def gacha_cost(level):
    return GACHA_COSTS[min(level, len(GACHA_COSTS)) - 1]


POST_MAX_LEVEL_GROWTH_RATE = TOWN_UPGRADE_COSTS[-1] / TOWN_UPGRADE_COSTS[-2]  # ≈ 1.2661


def town_upgrade_cost(level, costs=None):
    costs = costs if costs is not None else TOWN_UPGRADE_COSTS
    if level <= len(costs):
        return costs[level - 1]
    extra_levels = level - len(costs)
    return costs[-1] * (POST_MAX_LEVEL_GROWTH_RATE ** extra_levels)


def efficiency_multiplier(town_level):
    return 1 + (town_level - 1) * EFFICIENCY_BUFF_PER_LEVEL


def level_multiplier(level):
    return 1 + (level - 1) * LEVEL_BONUS_RATE


def levelup_cost(base_cost, level):
    return base_cost * (LEVELUP_COST_RATE ** (level - 1))


def dup_required(level):
    return DUP_GROWTH ** level


def roll_item(is_animal, town_level, difficulty, rng):
    # 해금된 종류끼리만 (실제 개별 확률값을) 재정규화해서 뽑음 (잠긴 종류는 애초에 나오지 않음)
    roster = ANIMALS if is_animal else TOOLS
    pool = unlocked_pool(roster, town_level, difficulty)
    total_prob = sum(x["indiv_prob"] for x in pool)
    r = rng.random() * total_prob
    cum = 0.0
    for x in pool:
        cum += x["indiv_prob"]
        if r <= cum:
            return x
    return pool[-1]


class Owned:
    __slots__ = ("level", "dup")

    def __init__(self):
        self.level = 1
        self.dup = 1


def simulate_once(town_upgrade_costs, rng, difficulty="Normal", max_hours=500):
    coins = 0.0
    time_s = 0.0
    town_level = 1
    difficulty_mult = DIFFICULTY_MULT_TABLE[difficulty]

    owned_animals = {}   # id -> Owned
    owned_tools = {}     # id -> Owned
    placed_animal_ids = []
    placed_tool_ids = []

    tool_eff_level = 0
    click_level = 0
    typing_level = 0

    def _all_units():
        # 페어(동물+도구, 특화매칭) 또는 단독 동물/단독 도구를 하나의 "생산 유닛"으로 모읍니다.
        # 시크릿 동물은 짝꿍 도구가 없으므로 TOOL_PAIR_OF_ANIMAL에 항목이 없을 수 있습니다.
        paired_tool_ids = set()
        units = []
        placed_tool_set = set(placed_tool_ids)
        for aid in placed_animal_ids:
            a = ANIMAL_BY_ID[aid]
            oa = owned_animals[aid]
            animal_prod = a["prod"] * level_multiplier(oa.level)
            expected_tid = TOOL_PAIR_OF_ANIMAL.get(aid)
            if expected_tid is not None and expected_tid in placed_tool_set and expected_tid not in paired_tool_ids:
                t = TOOL_BY_ID[expected_tid]
                ot = owned_tools[expected_tid]
                tool_prod = t["prod"] * level_multiplier(ot.level)
                units.append(((animal_prod + tool_prod) * (1 + SPECIAL_BONUS), aid, expected_tid))
                paired_tool_ids.add(expected_tid)
            else:
                units.append((animal_prod, aid, None))
        for tid in placed_tool_ids:
            if tid not in paired_tool_ids:
                t = TOOL_BY_ID[tid]
                ot = owned_tools[tid]
                units.append((t["prod"] * level_multiplier(ot.level), None, tid))
        return units

    def _active_units():
        units = _all_units()
        units.sort(key=lambda u: u[0], reverse=True)
        cap = tool_capacity(town_level)
        return units[:cap], units

    def production_rate():
        active, _all = _active_units()
        total = sum(u[0] for u in active)
        return total * (1 + tool_eff_level * TOOL_EFF_BONUS_PER_LEVEL) * difficulty_mult

    def active_ids():
        active, _all = _active_units()
        animal_ids = {u[1] for u in active if u[1] is not None}
        tool_ids = {u[2] for u in active if u[2] is not None}
        return animal_ids, tool_ids

    def active_cutoff():
        active, all_units = _active_units()
        cap = tool_capacity(town_level)
        if len(all_units) < cap:
            return 0.0
        return active[-1][0] if active else 0.0

    def click_typing_cap_per_sec():
        cap_per_hour = 10000 + click_level * CLICK_CAP_BONUS_PER_LEVEL + typing_level * TYPING_CAP_BONUS_PER_LEVEL
        return cap_per_hour / 3600.0

    def advance_to(cost):
        nonlocal coins, time_s
        if coins >= cost:
            return
        rate = production_rate() + click_typing_cap_per_sec() * CLICK_UTILIZATION
        needed = cost - coins
        dt = needed / rate
        time_s += dt
        coins = cost

    def roll_item_with_pity(is_animal):
        roster = ANIMALS if is_animal else TOOLS
        pool = unlocked_pool(roster, town_level, difficulty)
        legend_pool = [x for x in pool if x["grade"] == "Legendary"]

        if PITY_ROLLS > 0 and legend_pool and pity_counters[is_animal] >= PITY_ROLLS:
            item = legend_pool[rng.randrange(len(legend_pool))]
        else:
            item = roll_item(is_animal, town_level, difficulty, rng)

        if legend_pool:
            if item["grade"] == "Legendary":
                pity_counters[is_animal] = 0
            else:
                pity_counters[is_animal] += 1
        return item

    def gacha_roll(is_animal):
        nonlocal coins, total_refund
        item = roll_item_with_pity(is_animal)
        owned = owned_animals if is_animal else owned_tools
        placed_ids = placed_animal_ids if is_animal else placed_tool_ids
        if item["id"] in owned:
            owned[item["id"]].dup += 1
            if DUP_REFUND_RATE > 0:
                refund = gacha_cost(town_level) * DUP_REFUND_RATE
                coins += refund
                total_refund += refund
        else:
            owned[item["id"]] = Owned()
            placed_ids.append(item["id"])

    def marginal_gain_per_roll(is_animal):
        owned = owned_animals if is_animal else owned_tools
        roster = ANIMALS if is_animal else TOOLS
        pool = unlocked_pool(roster, town_level, difficulty)
        total_prob = sum(x["indiv_prob"] for x in pool)
        if total_prob == 0:
            return 0.0, 0.0
        cutoff = active_cutoff()
        expected = 0.0
        owned_prob = 0.0
        for x in pool:
            if x["id"] not in owned:
                effective_prod = x["prod"] if x["prod"] > cutoff else 0.0
                expected += (x["indiv_prob"] / total_prob) * effective_prod
            else:
                owned_prob += x["indiv_prob"] / total_prob
        return expected, owned_prob

    def tool_eff_gain():
        if tool_eff_level >= upgrade_max_level(town_level):
            return 0.0
        current_mult = 1 + tool_eff_level * TOOL_EFF_BONUS_PER_LEVEL
        return production_rate() / current_mult * TOOL_EFF_BONUS_PER_LEVEL

    def click_gain():
        if click_level >= upgrade_max_level(town_level):
            return 0.0
        return CLICK_CAP_BONUS_PER_LEVEL / 3600.0 * CLICK_UTILIZATION

    def typing_gain():
        if typing_level >= upgrade_max_level(town_level):
            return 0.0
        return TYPING_CAP_BONUS_PER_LEVEL / 3600.0 * CLICK_UTILIZATION

    def production_gain_from_town_upgrade():
        fallback = tool_eff_gain()
        active, all_units = _active_units()
        cap = tool_capacity(town_level)
        if len(all_units) > cap:
            benched_best = all_units[cap][0]
            capacity_gain = benched_best * (1 + tool_eff_level * TOOL_EFF_BONUS_PER_LEVEL) * difficulty_mult
            return max(fallback, capacity_gain)
        return fallback

    def try_level_ups():
        nonlocal total_levelups
        progressed = True
        while progressed:
            progressed = False
            active_animal_ids, active_tool_ids = active_ids()
            for owned_dict, active_id_set, by_id in (
                (owned_animals, active_animal_ids, ANIMAL_BY_ID), (owned_tools, active_tool_ids, TOOL_BY_ID)
            ):
                for oid, o in owned_dict.items():
                    if oid not in active_id_set:
                        continue
                    if not ENDLESS_MODE and o.level >= ANIMAL_TOOL_MAX_LEVEL:
                        continue
                    req = dup_required(o.level)
                    cost = levelup_cost(by_id[oid]["levelup_base_cost"], o.level)
                    if o.dup >= req and coins_avail() >= cost:
                        spend(cost)
                        o.dup -= req
                        o.level += 1
                        progressed = True
                        total_levelups += 1

    def coins_avail():
        return coins

    def spend(amount):
        nonlocal coins
        coins -= amount

    def buy_tool_eff():
        nonlocal tool_eff_level, total_tool_eff_buys
        cost = tool_eff_cost(tool_eff_level)
        advance_to(cost)
        spend(cost)
        tool_eff_level += 1
        total_tool_eff_buys += 1

    def buy_click():
        nonlocal click_level, total_click_buys
        cost = click_upgrade_cost(click_level)
        advance_to(cost)
        spend(cost)
        click_level += 1
        total_click_buys += 1

    def buy_typing():
        nonlocal typing_level, total_typing_buys
        cost = typing_upgrade_cost(typing_level)
        advance_to(cost)
        spend(cost)
        typing_level += 1
        total_typing_buys += 1

    level_reach_hours = {1: 0.0}
    total_gacha_rolls = 0
    total_levelups = 0
    total_refund = 0.0
    pity_counters = {True: 0, False: 0}
    total_tool_eff_buys = 0
    total_click_buys = 0
    total_typing_buys = 0
    production_rate_by_level = {}

    while town_level < 10 and time_s / 3600.0 < max_hours:
        while True:
            g_cost = gacha_cost(town_level)
            u_cost = town_upgrade_cost(town_level, town_upgrade_costs)

            a_gain, a_dup_prob = marginal_gain_per_roll(True)
            t_gain, t_dup_prob = marginal_gain_per_roll(False)
            a_eff_cost = g_cost * (1 - a_dup_prob * DUP_REFUND_RATE)
            t_eff_cost = g_cost * (1 - t_dup_prob * DUP_REFUND_RATE)
            a_payback = a_eff_cost / a_gain if a_gain > 0 else float("inf")
            t_payback = t_eff_cost / t_gain if t_gain > 0 else float("inf")

            te_cost = tool_eff_cost(tool_eff_level)
            te_gain = tool_eff_gain()
            te_payback = te_cost / te_gain if te_gain > 0 else float("inf")

            cl_cost = click_upgrade_cost(click_level)
            cl_gain = click_gain()
            cl_payback = cl_cost / cl_gain if cl_gain > 0 else float("inf")

            ty_cost = typing_upgrade_cost(typing_level)
            ty_gain = typing_gain()
            ty_payback = ty_cost / ty_gain if ty_gain > 0 else float("inf")

            u_gain = production_gain_from_town_upgrade()
            u_payback = u_cost / u_gain if u_gain > 0 else float("inf")

            options = [
                ("animal", a_payback), ("tool", t_payback),
                ("tool_eff", te_payback), ("click", cl_payback), ("typing", ty_payback),
            ]
            best_name, best_payback = min(options, key=lambda o: o[1])
            if best_payback >= u_payback:
                break

            if best_name == "animal":
                advance_to(g_cost); spend(g_cost); gacha_roll(True); total_gacha_rolls += 1
            elif best_name == "tool":
                advance_to(g_cost); spend(g_cost); gacha_roll(False); total_gacha_rolls += 1
            elif best_name == "tool_eff":
                buy_tool_eff()
            elif best_name == "click":
                buy_click()
            else:
                buy_typing()
            try_level_ups()

        cost = town_upgrade_cost(town_level, town_upgrade_costs)
        advance_to(cost)
        spend(cost)
        production_rate_by_level[town_level] = production_rate()
        town_level += 1
        level_reach_hours[town_level] = time_s / 3600.0
        try_level_ups()

    level10_hours = time_s / 3600.0

    # 2026.08.06: 레벨10 도달 후 "재건(엔딩)" 버튼 - VillageSystemManager.villageCompletionCost.
    # 코인을 모아서 지불해야 리셋/엔드리스 선택 팝업이 열리므로, "완주"의 실제 끝은 여기입니다.
    advance_to(VILLAGE_COMPLETION_COST)
    spend(VILLAGE_COMPLETION_COST)
    ending_hours = time_s / 3600.0

    max_level_by_grade = {}
    leftover_dups = 0
    for owned_dict, by_id in ((owned_animals, ANIMAL_BY_ID), (owned_tools, TOOL_BY_ID)):
        for oid, o in owned_dict.items():
            grade = by_id[oid]["grade"]
            max_level_by_grade[grade] = max(max_level_by_grade.get(grade, 1), o.level)
            leftover_dups += max(0, o.dup - 1)

    return {
        "hours": ending_hours,
        "level10_hours": level10_hours,
        "level_reach_hours": level_reach_hours,
        "total_gacha_rolls": total_gacha_rolls,
        "animals_collected": len(owned_animals),
        "tools_collected": len(owned_tools),
        "total_levelups": total_levelups,
        "leftover_dups": leftover_dups,
        "total_refund": total_refund,
        "final_production": production_rate(),
        "production_rate_by_level": production_rate_by_level,
        "max_level_by_grade": max_level_by_grade,
        "tool_eff_level": tool_eff_level,
        "click_level": click_level,
        "typing_level": typing_level,
        "total_tool_eff_buys": total_tool_eff_buys,
        "total_click_buys": total_click_buys,
        "total_typing_buys": total_typing_buys,
    }


def run_trials(town_upgrade_costs, difficulty="Normal", trials=200, max_hours=500):
    return [simulate_once(town_upgrade_costs, random.Random(1000 + i), difficulty=difficulty, max_hours=max_hours)
            for i in range(trials)]


def average_completion_hours(town_upgrade_costs, difficulty="Normal", trials=200, max_hours=500):
    runs = run_trials(town_upgrade_costs, difficulty=difficulty, trials=trials, max_hours=max_hours)
    hours = [r["hours"] for r in runs]
    return statistics.mean(hours), statistics.pstdev(hours), hours


def report_for_difficulty(difficulty, N=1000):
    runs = run_trials(TOWN_UPGRADE_COSTS, difficulty=difficulty, trials=N, max_hours=5000)
    hours = [r["hours"] for r in runs]           # 레벨10 + 재건 버튼(엔딩) 비용 지불까지 전체
    level10_hours = [r["level10_hours"] for r in runs]  # 레벨10 도달까지만(재건 비용 제외)
    print("=" * 70)
    print("난이도:", difficulty, " (생산 배율 x", DIFFICULTY_MULT_TABLE[difficulty], ")")
    print("--- 레벨10 도달까지(재건 비용 제외) ---")
    print("avg:", statistics.mean(level10_hours), "std:", statistics.pstdev(level10_hours))
    print("--- 완주(레벨10 + 재건 버튼", f"{VILLAGE_COMPLETION_COST:,}", "코인) ---")
    print("avg hours:", statistics.mean(hours), "std:", statistics.pstdev(hours))
    print("min/max:", min(hours), max(hours))
    print("median:", statistics.median(hours))

    total = statistics.mean(hours)
    prev = 0.0
    shares = []
    for lvl in range(1, 11):
        vals = [r["level_reach_hours"].get(lvl) for r in runs if lvl in r["level_reach_hours"]]
        avgv = statistics.mean(vals)
        share = (avgv - prev) / total
        shares.append(share)
        print(f"Level {lvl:2}: avg={avgv:7.2f}h  share={share*100:5.2f}%  reached_by={len(vals)}/{N}")
        prev = avgv
    ending_avg = statistics.mean(hours)
    ending_share = (ending_avg - prev) / total
    print(f"엔딩(재건): avg={ending_avg:7.2f}h  share={ending_share*100:5.2f}%")

    monotonic = all(shares[i] <= shares[i + 1] + 1e-6 for i in range(1, 9))
    print("monotonic(레벨1~10 구간):", monotonic, " levels 8-10 share:", sum(shares[7:10]) * 100, "%")
    print("avg total gacha rolls:", statistics.mean(r["total_gacha_rolls"] for r in runs))
    print("avg animals collected:", statistics.mean(r["animals_collected"] for r in runs))
    print("avg tools collected (of 40):", statistics.mean(r["tools_collected"] for r in runs))
    print("avg final_production (coins/sec):", statistics.mean(r["final_production"] for r in runs))
    return {
        "difficulty": difficulty,
        "avg_level10_hours": statistics.mean(level10_hours),
        "avg_hours": statistics.mean(hours),
        "std_hours": statistics.pstdev(hours),
        "median_hours": statistics.median(hours),
        "min_hours": min(hours),
        "max_hours": max(hours),
        "monotonic": monotonic,
        "levels_8_10_share": sum(shares[7:10]) * 100,
        "ending_share": ending_share * 100,
        "avg_gacha_rolls": statistics.mean(r["total_gacha_rolls"] for r in runs),
        "avg_animals_collected": statistics.mean(r["animals_collected"] for r in runs),
        "avg_tools_collected": statistics.mean(r["tools_collected"] for r in runs),
        "avg_final_production": statistics.mean(r["final_production"] for r in runs),
        "level_reach_hours_avg": [
            statistics.mean([r["level_reach_hours"].get(lvl) for r in runs if lvl in r["level_reach_hours"]])
            for lvl in range(1, 11)
        ],
    }


if __name__ == "__main__":
    import json
    results = {}
    for diff in ("Normal", "Hard", "VeryHard"):
        results[diff] = report_for_difficulty(diff, N=1000)
        print()

    with open("sim_results_difficulty.json", "w", encoding="utf-8") as f:
        json.dump(results, f, ensure_ascii=False, indent=2)
    print("saved sim_results_difficulty.json")
