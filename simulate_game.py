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
GRADES = [
    {"name": "Normal",    "count": 12, "prob": 0.45, "prod": 1},
    {"name": "Rare",      "count": 10, "prob": 0.30, "prod": 2},
    {"name": "Epic",      "count": 8,  "prob": 0.15, "prod": 4},
    {"name": "Unique",    "count": 6,  "prob": 0.08, "prod": 8},
    {"name": "Legendary", "count": 4,  "prob": 0.02, "prod": 34},
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


def unlocked_pool(roster, town_level):
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
        g = item["grade"]
        if counters[g] < limits[g]:
            result.append(item)
            counters[g] += 1
    return result

# #13: 도구 수치(등급 분포/생산량)는 동물과 1:1 미러링으로 확정 - 사용자 확인 완료.
# 도구 이름/설명 등 텍스트만 추후 추가 예정이며 밸런스 수치에는 영향 없음.
# 도구는 동물과 1:1 비율로 생성 (같은 등급 분포, 생산량은 동물과 동일하게 미러링)
def build_roster():
    animals, tools = [], []
    for g in GRADES:
        indiv_prob = g["prob"] / g["count"]
        for i in range(g["count"]):
            aid = f"A_{g['name']}_{i}"
            tid = f"T_{g['name']}_{i}"
            animals.append({"id": aid, "grade": g["name"], "prod": g["prod"], "indiv_prob": indiv_prob})
            tools.append({"id": tid, "grade": g["name"], "prod": g["prod"], "indiv_prob": indiv_prob, "pair": aid})
    return animals, tools

ANIMALS, TOOLS = build_roster()
TOOL_PAIR_OF_ANIMAL = {t["pair"]: t["id"] for t in TOOLS}
ANIMAL_BY_ID = {a["id"]: a for a in ANIMALS}
TOOL_BY_ID = {t["id"]: t for t in TOOLS}

# ---------------------------------------------------------------
# 게임 규칙 파라미터 (실제 코드 기본값 + 조정 대상 성장률)
# ---------------------------------------------------------------
LEVEL_BONUS_RATE = 0.2          # AnimalData/ToolData - levelBonusRatePerLevel
SPECIAL_BONUS = 0.5             # ToolData.specialAnimalBonusRate
# #10 리밸런스: 4^레벨(4/16/64/256)은 코인 타이밍 문제(항상 0원)와 겹쳐 레벨업이 사실상
# 불가능했음. 환급/천장으로 코인 타이밍은 고쳤지만, 중복 요구치 자체도 2^레벨(2/4/8/16)로
# 낮춰서 레벨업 빈도와 희귀 등급 진행도를 함께 개선함 (사용자 확인 후 확정).
DUP_GROWTH = 2                   # LevelUpRequirementCalculator (2^레벨)
LEVELUP_BASE_COST = 100
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
# 목표 구간 비중: 5/6/8/9/10/12/15/17/18% (레벨2~10, 합 100%, 레벨 8~10 합 50%) x 목표 총 50시간.
# 초반(2→3구간, 4.55배)과 막판(9→10구간, 2.58배)에 배율이 큰 건 계산 실수가 아니라,
# 레벨1에 노말+레어를 몰아서 풀어준 것과 Legendary가 레벨9 근처에 몰려 해금되는 것 때문에
# 생기는 구조적인 현상입니다 (사용자 확인 후 반올림 값으로 확정).
# #10 리밸런스: DUP_GROWTH를 4->2로 낮추면서(아래 참고) 레벨업이 더 쉬워져 완주가
# 빨라진 만큼, 같은 목표 구간 소요시간(2.5~9.0h, 합 50h)에 맞춰 다시 역산.
# #11 리밸런스: Legendary 생산량을 40->34로 낮추면서(아래 GRADES 참고) 완주가 살짝
# 느려진 만큼, 같은 목표 구간 소요시간에 맞춰 다시 역산.
# #15: 사용자 요청으로 1만 단위로 깔끔하게 반올림 (완주시간 48.74h->48.73h로 사실상 동일).
# #18 리밸런스(도구 상한 기능 추가, 이슈 #72 후속): 도구 상한(TOOL_CAPACITY_*)과 위 인터리빙
# 해금 스케줄이 추가되면서 이 비용 곡선도 다시 역산했습니다. 처음엔 목표 총 70h를 잡았지만,
# 상한 도입 후 경제구조에서는 "완전한 단조증가"와 "정확히 70h"를 동시에 만족하는 지점이
# 없었습니다(구간 하나를 늘리면 다른 구간이 깨지는 두더지잡기 현상을 좌표하강법/스케일링/
# 구간별 수동보정으로 반복 확인). 완전한 단조증가가 자연스럽게 성립하는 지점을 찾은 결과
# 총 78.6h로 확정(N=1000, avg=78.61h, std=1.67h, 레벨8~10 비중 46.8%, 사용자 확인 완료).
# #19 리밸런스: 클릭/타이핑/도구효율 업그레이드 상한을 고정 20에서 min(마을레벨, 10)으로
# 바꾸고(사용자 확인), 그동안 시뮬레이션에 빠져있던 동물/도구 개별 레벨5 상한(실제 코드
# SlotData_Tool.ToolLevelUp 기준)을 처음 반영하면서 다시 역산했습니다. 업그레이드 상한이
# 낮아지며 돈이 마을업그레이드/뽑기로 더 몰려서 완주시간이 78.61h -> 59.95h로 단축됨
# (레벨9->10 구간만 소폭 상향해서 단조증가 유지, N=1000 검증 완료).
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
# EFFICIENCY_BUFF_PER_LEVEL은 도구 효율 업그레이드의 레벨당 배율로 그대로 재사용합니다
# (마을 레벨이 아니라 "구매한 도구 효율 레벨"에 곱해짐).
EFFICIENCY_BUFF_PER_LEVEL = 0.1

# ---------------------------------------------------------------
# #17 리밸런스(이슈 #72): 클릭 코인/타이핑 코인/도구 효율 개별 업그레이드.
# 기존에는 마을 레벨이 오르면 자동으로 생산량에 배율이 붙었지만(위 EFFICIENCY_BUFF_PER_LEVEL을
# 마을 레벨에 곱함), 이제는 도구 효율 업그레이드를 직접 구매해야만 그 배율이 오릅니다.
# 클릭/타이핑 업그레이드는 EarnProcessor의 배율(ClickMultiplier/TypingMultiplier)뿐 아니라
# 시간당 획득 상한(maxCoinPerHour)도 함께 올려서, "이미 상한을 채우는 플레이어"에게도 실질적인
# 생산량 증가 효과를 갖도록 설계했습니다(상한이 배율과 무관하게 고정이면 상한을 채우는 플레이어는
# 배율을 올려도 얻는 게 없어서 - 사용자 확인 후 상한도 함께 올리는 방향으로 확정).
#
# 셋 다 "효율적 지출" 봇의 선택지에 추가해서, 뽑기/마을업그레이드와 회수시간(payback)을 비교하며
# 가장 유리한 곳에 코인을 쓰도록 했습니다. 아래 비용/효과 값은 N=1,000 시뮬레이션으로 목표
# 완주시간(약 60h, 레벨8~10 비중 50%, 단조증가)을 유지하면서 세 업그레이드가 실제로 유의미한
# 빈도로 구매되도록 역산/조정한 값입니다.
TOOL_EFF_BASE_COST = 60_000
TOOL_EFF_COST_GROWTH = 1.6
TOOL_EFF_BONUS_PER_LEVEL = EFFICIENCY_BUFF_PER_LEVEL  # 레벨당 +10% 생산 배율 (기존 자동 버프와 동일 폭)

# 클릭/타이핑은 초반 부트스트랩(자동 생산이 거의 없을 때) 위주로 의미 있고, 생산량이 커지면
# 시간당 상한 자체가 상대적으로 작아져서 봇이 자연스럽게 더 이상 안 사게 됩니다(의도된 설계).
# #17 튜닝: growth 1.35/1.6 조합 등 여러 값을 N=150~300으로 스윕한 결과, 도구효율/클릭/타이핑
# 성장률을 모두 1.6으로 통일했을 때만 단조증가(share never decreases)가 안정적으로 유지됨
# (하나라도 더 낮으면 해당 업그레이드가 중후반에 몰아 사는 구간이 생겨 레벨9->10 구간이
# 비정상적으로 짧아지는 비단조 현상 발생 - N=300 검증 완료).
CLICK_UPGRADE_BASE_COST = 300
CLICK_UPGRADE_COST_GROWTH = 1.6
CLICK_CAP_BONUS_PER_LEVEL = 2000   # 코인/시간, 레벨당 시간당 상한 증가분

TYPING_UPGRADE_BASE_COST = 300
TYPING_UPGRADE_COST_GROWTH = 1.6
TYPING_CAP_BONUS_PER_LEVEL = 2000  # 코인/시간, 레벨당 시간당 상한 증가분

# #19(신규): 실제 유니티 구현(TownUpgradeManager)과 동일하게 레벨 상한을 둡니다.
# 기존엔 마을 레벨과 무관하게 고정 20이었으나, 마을 레벨과 나란히 올라가다 10에서 같이
# 멈추는 구조로 변경(사용자 확인 완료) - 엔드리스 모드 설계의 사전 작업.
UPGRADE_MAX_LEVEL_CAP = 10   # 절대 상한(일반 모드)
ENDLESS_MODE = False          # True면 아래 상한들이 전부 해제됨(엔드리스 모드용)


def upgrade_max_level(town_level):
    if ENDLESS_MODE:
        return float("inf")
    return min(town_level, UPGRADE_MAX_LEVEL_CAP)


# 실제 코드(SlotData_Tool.ToolLevelUp/SlotData_Animal 동일)는 개별 동물/도구 레벨을 5에서
# 막는데, 이 시뮬레이션은 지금까지 이 상한을 전혀 반영하지 않고 있었음(발견 및 수정, #19).
# 다만 기존 결과 통계(등급별 평균 최고 도달 레벨 3~5대)를 보면 "효율적 지출" 봇이 애초에
# 5를 크게 못 넘기고 있어서 78.6h 설계에 미치는 영향은 작을 것으로 예상 - 재검증으로 확인.
ANIMAL_TOOL_MAX_LEVEL = 5

# ---------------------------------------------------------------
# #18(신규): 도구 상한(동시에 "동물이 장착된 도구" 슬롯 개수 제한). 실제 게임 생산 로직
# (RealProductionTicker)은 도구 슬롯 중 CurrentAnimalSet==true인 것만 생산하므로, 이 상한은
# 사실상 "동시에 생산 가능한 동물+도구 쌍의 개수"입니다. 마을 레벨을 올리면 상한이 늘어납니다.
# 레벨10은 엔딩(완주 시점)이라 시뮬레이션이 그 순간 종료되므로 tool_capacity(10)은 실제로
# 쓰이지 않습니다(9->10 구간까지만 적용) - 사용자 확인 완료.
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


# ---------------------------------------------------------------
# #9 리밸런스: 죽은 재고(레벨업이 사실상 안 되는 문제) 개선 메커니즘 + 민감도 실험용 파라미터
# ---------------------------------------------------------------
# 중복 환급: 이미 보유한 종류를 다시 뽑으면 현재 뽑기 비용의 이 비율만큼 코인 환급
DUP_REFUND_RATE = 0.3
# Legendary 천장(pity): Legendary가 해금된 상태에서 이 횟수 동안 Legendary가 안 나오면
# 다음 뽑기는 해금된 Legendary 중 하나 확정
PITY_ROLLS = 100
# 클릭/타이핑 상한 실제 달성률 (1.0 = 매시간 상한 완주라는 기존의 낙관 가정, 민감도 분석용)
CLICK_UTILIZATION = 1.0
# #12 리밸런스: 문제#6(완주 후 인플레이션) 해결 방향 확정 - 레벨10 완주("엔딩") 시 코인/마을레벨/
# 보유 동물·도구/도감을 전부 초기화하고 다음 난이도로 재도전하는 구조로 기획 확정. 코인이 쌓일 시간
# 자체가 없어지므로 별도 코인 싱크 없이 인플레이션 문제가 해소됨. 남은 건 난이도별 재도전이 실제로
# 유의미하게 오래 걸리는지 검증하는 것뿐 - DIFFICULTY_MULT가 그 "다음 난이도" 배율.
# Easy(x1.2)/Normal(x1.0)/Hard(x0.8)/VeryHard(x0.6) 검증 결과(N=1000, 최종 파라미터 기준):
# 42.00h -> 50.25h -> 62.58h -> 83.00h, 전 구간 단조증가 + 레벨8~10 비중 50%대 유지,
# VeryHard/Easy 완주시간 비율 1.98배(배율 역수 2.0배와 거의 일치) - 난이도 간 체감 격차가
# 생산 배율에 비례해서 예측 가능하게 늘어남을 확인.
DIFFICULTY_MULT = 1.0


def gacha_cost(level):
    return GACHA_COSTS[min(level, len(GACHA_COSTS)) - 1]


# #19(엔드리스 사전 작업): 실제 유니티 구현(TownUpgradeCostConfig.GetCostForTownLevel)과 동일하게,
# 정의된 표(레벨9까지)를 넘어서면 레벨9->10 구간의 실제 성장률을 그대로 반복해서 계속 늘어나도록 함
# (사용자 확인: "레벨9->10 배율을 그대로 반복"). 정의된 범위 안에서는 기존과 동일하게 표 값을 그대로 씀.
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


def levelup_cost(level):
    return LEVELUP_BASE_COST * (LEVELUP_COST_RATE ** (level - 1))


def dup_required(level):
    return DUP_GROWTH ** level


def roll_item(is_animal, town_level, rng):
    # 해금된 종류끼리만 (실제 개별 확률값을) 재정규화해서 뽑음 (잠긴 종류는 애초에 나오지 않음)
    roster = ANIMALS if is_animal else TOOLS
    pool = unlocked_pool(roster, town_level)
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


def simulate_once(town_upgrade_costs, rng, max_hours=500):
    coins = 0.0
    time_s = 0.0
    town_level = 1

    owned_animals = {}   # id -> Owned
    owned_tools = {}     # id -> Owned
    placed_animal_ids = []
    placed_tool_ids = []

    # #17(이슈 #72): 마을 레벨 자동 보너스를 대체하는 구매형 업그레이드 레벨들
    tool_eff_level = 0
    click_level = 0
    typing_level = 0

    def _all_units():
        # 페어(동물+도구, 특화매칭) 또는 단독 동물/단독 도구를 하나의 "생산 유닛"으로 모읍니다.
        # 각 유닛을 (생산량, 동물id_또는_None, 도구id_또는_None)로 반환합니다.
        paired_tool_ids = set()
        units = []
        placed_tool_set = set(placed_tool_ids)
        for aid in placed_animal_ids:
            a = ANIMAL_BY_ID[aid]
            oa = owned_animals[aid]
            animal_prod = a["prod"] * level_multiplier(oa.level)
            expected_tid = TOOL_PAIR_OF_ANIMAL[aid]
            if expected_tid in placed_tool_set and expected_tid not in paired_tool_ids:
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
        # #18: 도구 상한 - 생산 유닛을 값 기준 내림차순 정렬해서 상위 tool_capacity(town_level)개만
        # "활성"(실제로 생산에 반영)으로 취급합니다. 실제 플레이어라면 당연히 제일 좋은 조합을
        # 골라서 끼울 것이므로, 항상 상위 N개가 활성이라고 가정합니다(효율적 지출 봇과 동일한 전제).
        units = _all_units()
        units.sort(key=lambda u: u[0], reverse=True)
        cap = tool_capacity(town_level)
        return units[:cap], units

    def production_rate():
        active, _all = _active_units()
        total = sum(u[0] for u in active)
        return total * (1 + tool_eff_level * TOOL_EFF_BONUS_PER_LEVEL) * DIFFICULTY_MULT

    def active_ids():
        # 현재 활성(생산 중) 슬롯에 들어있는 동물/도구 id 집합 - 레벨업 대상 판단에 사용
        active, _all = _active_units()
        animal_ids = {u[1] for u in active if u[1] is not None}
        tool_ids = {u[2] for u in active if u[2] is not None}
        return animal_ids, tool_ids

    def active_cutoff():
        # 지금 활성 슬롯 중 가장 약한 유닛의 생산량(= 새 유닛이 이 값을 넘어야 상한 안에 들어감).
        # 아직 상한을 다 못 채웠으면 0(뭘 얻어도 바로 활성화됨).
        active, all_units = _active_units()
        cap = tool_capacity(town_level)
        if len(all_units) < cap:
            return 0.0
        return active[-1][0] if active else 0.0

    def click_typing_cap_per_sec():
        # 기본 상한(1만/시간) + 클릭/타이핑 업그레이드로 늘어난 상한. 상한 자체가 오르므로
        # 이미 상한을 채우는 플레이어(CLICK_UTILIZATION=1.0)에게도 실질적인 이득이 됩니다.
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
        # Legendary 천장: 해금된 Legendary가 있는 상태에서 PITY_ROLLS 동안 안 나오면 확정 지급
        roster = ANIMALS if is_animal else TOOLS
        pool = unlocked_pool(roster, town_level)
        legend_pool = [x for x in pool if x["grade"] == "Legendary"]

        if PITY_ROLLS > 0 and legend_pool and pity_counters[is_animal] >= PITY_ROLLS:
            item = legend_pool[rng.randrange(len(legend_pool))]
        else:
            item = roll_item(is_animal, town_level, rng)

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
            # 새 종류를 얻으면 그 즉시 슬롯도 함께 생겨서 바로 배치됨
            owned[item["id"]] = Owned()
            placed_ids.append(item["id"])

    def marginal_gain_per_roll(is_animal):
        # 이번 뽑기 1회가 기대할 수 있는 "새 종류 획득으로 인한 생산량 증가분" (레벨1 기준 base prod)과
        # 중복이 나올 확률을 함께 반환. 해금된 종류끼리만 확률을 재정규화 (roll_item과 동일한 풀)
        # #18: 도구 상한 때문에, 새로 얻는 종류의 기본 생산량이 지금 활성 슬롯 중 가장 약한 유닛보다
        # 낮으면 어차피 상한 밖으로 밀려나 생산에 반영되지 않으므로 기대 이득을 0으로 봅니다
        # (효율적 지출 봇이 상한이 꽉 찬 뒤에는 낮은 등급 뽑기를 그만 가치 있게 여기게 되는 이유).
        owned = owned_animals if is_animal else owned_tools
        roster = ANIMALS if is_animal else TOOLS
        pool = unlocked_pool(roster, town_level)
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
        # 도구 효율 업그레이드 1레벨이 늘려주는 생산량 증가분 (현재 배율 기준 marginal)
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
        # #17: 자동 생산 버프는 폐지되어 마을 업그레이드 자체엔 직접적인 생산 증가가 없습니다.
        # #18: 대신 마을 레벨업은 도구 상한을 늘려줘서, 지금 상한 밖(벤치)에 있는 유닛 중 제일
        # 강한 걸 바로 활성화시켜주는 실질적 효과가 생겼습니다 - 그 유닛의 생산량을 "마을
        # 업그레이드의 기대 이득"으로 사용합니다(실제 메커니즘에 기반한 값이라 #17때 썼던
        # 도구효율 프록시보다 안정적).
        # 다만 아직 상한을 다 못 채웠으면(벤치가 없으면) 이 값이 0이 되어 "효율적 지출" 봇이
        # 재량 지출(도구효율 등)을 무한정 우선시하는 문제가 생기므로, 도구 효율 업그레이드의
        # 기대 이득을 최소 기준점(fallback)으로 함께 반영해 항상 0보다 큰 판단 기준을 보장합니다.
        fallback = tool_eff_gain()
        active, all_units = _active_units()
        cap = tool_capacity(town_level)
        if len(all_units) > cap:
            benched_best = all_units[cap][0]
            capacity_gain = benched_best * (1 + tool_eff_level * TOOL_EFF_BONUS_PER_LEVEL) * DIFFICULTY_MULT
            return max(fallback, capacity_gain)
        return fallback

    def try_level_ups():
        # 저장한 코인 한도 내에서 가능한 레벨업을 반복 수행 (동물/도구 번갈아 시도).
        # #18: 도구 상한 때문에 지금 활성(생산 중)이 아닌 벤치 유닛을 레벨업하는 건 낭비이므로,
        # 활성 슬롯에 들어있는 동물/도구만 레벨업 대상으로 삼습니다(효율적 지출 봇 전제).
        nonlocal total_levelups
        progressed = True
        while progressed:
            progressed = False
            active_animal_ids, active_tool_ids = active_ids()
            for owned_dict, active_id_set in (
                (owned_animals, active_animal_ids), (owned_tools, active_tool_ids)
            ):
                for oid, o in owned_dict.items():
                    if oid not in active_id_set:
                        continue
                    if not ENDLESS_MODE and o.level >= ANIMAL_TOOL_MAX_LEVEL:
                        continue
                    req = dup_required(o.level)
                    cost = levelup_cost(o.level)
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
    # #17(이슈 #72): 새 개별 업그레이드 구매 횟수 집계용
    total_tool_eff_buys = 0
    total_click_buys = 0
    total_typing_buys = 0
    # #16: 오프라인 보상 설계용 - 마을 레벨업 시점의 생산량(코인/초)을 기록
    production_rate_by_level = {}

    while town_level < 10 and time_s / 3600.0 < max_hours:
        # 1) "효율적 지출": 아래 5가지 선택지(동물뽑기/도구뽑기/도구효율/클릭/타이핑) 중 회수시간
        #    (payback = 비용/기대 생산량 증가분)이 가장 좋은 곳부터 계속 사고, 전부 마을 업그레이드
        #    저축보다 못해지면 멈춥니다. 수집이 진행될수록 뽑기의 기대 이득이 줄고(중복 확률 상승),
        #    개별 업그레이드도 레벨이 오를수록 비용이 기하급수적으로 늘어 자연히 비중이 줄어듭니다.
        while True:
            g_cost = gacha_cost(town_level)
            u_cost = town_upgrade_cost(town_level, town_upgrade_costs)

            a_gain, a_dup_prob = marginal_gain_per_roll(True)
            t_gain, t_dup_prob = marginal_gain_per_roll(False)
            # 중복 환급이 있으면 기대 환급만큼 뽑기의 유효 비용이 줄어든다 (효율적 지출 봇의 판단 기준)
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

        # 2) 마을 업그레이드 비용 저축 후 업그레이드
        cost = town_upgrade_cost(town_level, town_upgrade_costs)
        advance_to(cost)
        spend(cost)
        production_rate_by_level[town_level] = production_rate()
        town_level += 1
        level_reach_hours[town_level] = time_s / 3600.0
        try_level_ups()

    # 등급별 도달 레벨/잉여 중복(본체 1개 제외 후 남은 재고) 집계 - 죽은 재고 분석용
    max_level_by_grade = {}
    leftover_dups = 0
    for owned_dict, by_id in ((owned_animals, ANIMAL_BY_ID), (owned_tools, TOOL_BY_ID)):
        for oid, o in owned_dict.items():
            grade = by_id[oid]["grade"]
            max_level_by_grade[grade] = max(max_level_by_grade.get(grade, 1), o.level)
            leftover_dups += max(0, o.dup - 1)

    return {
        "hours": time_s / 3600.0,
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


def run_trials(town_upgrade_costs, trials=200, max_hours=500):
    return [simulate_once(town_upgrade_costs, random.Random(1000 + i), max_hours=max_hours) for i in range(trials)]


def average_completion_hours(town_upgrade_costs, trials=200, max_hours=500):
    runs = run_trials(town_upgrade_costs, trials, max_hours)
    hours = [r["hours"] for r in runs]
    return statistics.mean(hours), statistics.pstdev(hours), hours


if __name__ == "__main__":
    N = 1000
    runs = run_trials(TOWN_UPGRADE_COSTS, trials=N, max_hours=5000)
    hours = [r["hours"] for r in runs]
    print("town_upgrade_costs:", TOWN_UPGRADE_COSTS)
    print("avg hours:", statistics.mean(hours), "std:", statistics.pstdev(hours))
    print("min/max:", min(hours), max(hours))
    print("median:", statistics.median(hours))

    print()
    print("--- per-level average hours to reach (across", N, "trials) ---")
    total = statistics.mean(hours)
    prev = 0.0
    shares = []
    for lvl in range(1, 11):
        vals = [r["level_reach_hours"].get(lvl) for r in runs if lvl in r["level_reach_hours"]]
        avgv = statistics.mean(vals)
        stdv = statistics.pstdev(vals) if len(vals) > 1 else 0.0
        share = (avgv - prev) / total
        shares.append(share)
        print(f"Level {lvl:2}: avg={avgv:7.2f}h  share={share*100:5.2f}%  std={stdv:.2f}h  "
              f"min={min(vals):.2f}h  max={max(vals):.2f}h  reached_by={len(vals)}/{N}")
        prev = avgv

    print()
    print("monotonic (share never decreases vs previous level):",
          all(shares[i] <= shares[i + 1] + 1e-6 for i in range(1, 9)))
    print("levels 8-10 share:", sum(shares[7:10]) * 100, "%")

    print()
    print("avg total gacha rolls:", statistics.mean(r["total_gacha_rolls"] for r in runs))
    print("avg animals collected (of 40):", statistics.mean(r["animals_collected"] for r in runs))
    print("avg tools collected (of 40):", statistics.mean(r["tools_collected"] for r in runs))

    print()
    print("--- #17(이슈 #72): 클릭/타이핑/도구효율 업그레이드 ---")
    print("avg tool_eff level reached:", statistics.mean(r["tool_eff_level"] for r in runs),
          "/ max", upgrade_max_level(10))
    print("avg click level reached:", statistics.mean(r["click_level"] for r in runs),
          "/ max", upgrade_max_level(10))
    print("avg typing level reached:", statistics.mean(r["typing_level"] for r in runs),
          "/ max", upgrade_max_level(10))
    print("avg tool_eff buys:", statistics.mean(r["total_tool_eff_buys"] for r in runs))
    print("avg click buys:", statistics.mean(r["total_click_buys"] for r in runs))
    print("avg typing buys:", statistics.mean(r["total_typing_buys"] for r in runs))

    import json
    with open("sim_results_v5.json", "w", encoding="utf-8") as f:
        json.dump({
            "solved_town_upgrade_costs": TOWN_UPGRADE_COSTS,
            "hours": hours,
            "level_reach_hours": [r["level_reach_hours"] for r in runs],
            "total_gacha_rolls": [r["total_gacha_rolls"] for r in runs],
            "animals_collected": [r["animals_collected"] for r in runs],
            "tools_collected": [r["tools_collected"] for r in runs],
            "tool_eff_level": [r["tool_eff_level"] for r in runs],
            "click_level": [r["click_level"] for r in runs],
            "typing_level": [r["typing_level"] for r in runs],
        }, f)
    print("saved sim_results_v5.json")
