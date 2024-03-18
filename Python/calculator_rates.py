import datetime
import math
from dateutil.relativedelta import relativedelta


startDay = datetime.datetime.now()
finalDay = None

ratePercent = 2
input_money = 50
cycle_month = 12


def calculator_addDayRate():
    global finalDay
    finalDay = startDay + relativedelta(months=cycle_month)
    print(datetime.datetime.now(), finalDay)

    global input_money
    input_money = money_manwon()

    dayRate = calculator_dayRate()
    print(f"{dayRate:.8f} : 하루이자 {input_money*dayRate:.3f}")

    final_money = 0
    for a in range(0, cycle_month, 1):
        final_money = input_money + money_oneCycle(a, final_money)

    # final_money = 0
    # for a in range(0, cycle_month, 1):
    #     pass


# 하루기준금리
def calculator_dayRate():
    rate = ratePercent / 100
    return rate / 365


# 입금단위
def money_manwon():
    return input_money * 10000


def money_oneCycle(index, money):
    newStartDay = startDay + relativedelta(months=index)
    days = (finalDay - newStartDay).days
    if index == 0:
        money = input_money

    final_money = money * ((1 + calculator_dayRate()) ** days)

    print(f">>> {index+1}달 {days}일 = {math.floor(final_money):,}")
    return final_money


calculator_addDayRate()
