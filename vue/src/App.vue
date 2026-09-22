<template>
  <div class="app-container">
    
    <!-- 1. 左側導覽列 -->
    <div class="sidebar">
      <h3>權證導覽列</h3>
      <input 
        type="text" 
        v-model="searchKeyword" 
        @input="fetchWarrants"
        placeholder="搜尋代碼..." 
        class="search-input"
      />
      <ul class="warrant-list">
        <li v-if="filteredWarrants.length === 0" style="text-align: center; color: #99; padding: 10px;">
          無符合資料
        </li>
        <li 
          v-for="w in filteredWarrants" 
          :key="w.warrant_ID"
          @click="selectWarrant(w)"
          :class="['warrant-item', { active: selectedWarrant?.warrant_ID === w.warrant_ID }]"
        >
          <span>{{ w.warrant_ID }}</span>
          <span>{{ w.warrant_Type }}</span>
        </li>
      </ul>
    </div>

    <!-- 2. 右側主工作區 -->
    <div class="main-content">
      
      <div v-if="!selectedWarrant" style="text-align: center; margin-top: 100px; color: #888;">
        請從左側選擇一檔權證進行試算
      </div>

      <template v-else>
        <!-- 試算面板 -->
        <div class="card">
          <h2>權證代碼: <span class="text-blue">{{ selectedWarrant.warrant_ID }}</span> ({{ selectedWarrant.warrant_Type }})</h2>
          <hr style="margin: 10px 0; border: 0; border-top: 1px solid #eee;" />
          
          <div class="info-grid">
            <div>履約價格: <strong>{{ selectedWarrant.strike_Price }}</strong></div>
            <div>行使比例: <strong>{{ selectedWarrant.conversion_Ratio }}</strong></div>
            <div>庫存張數: <strong>{{ selectedWarrant.position_Qty }} 張</strong></div>
          </div>

          <div class="action-row">
            <label>輸入標的股價:</label>
            <input 
              type="number" 
              v-model.number="marketPrice" 
              @input="handleCalculate(false)"
              placeholder="> 0"
            />
            <button @click="handleCalculate(true)" class="btn-save">儲存試算結果</button>
          </div>

          <div class="result-panel">
            <div class="result-item">
              <span>理論價值</span>
              <strong class="text-blue">{{ theoryPrice.toFixed(4) }}</strong>
            </div>
            <div class="result-item">
              <span>建議避險張數 (Delta Hedging)</span>
              <strong class="text-red">{{ hedgeQty.toFixed(2) }} 張</strong>
            </div>
          </div>
        </div>

        <!-- 3. 歷史明細區 -->
        <div class="card" style="flex: 1;">
          <h3>最近 10 筆存檔紀錄</h3>
          <table class="history-table">
            <thead>
              <tr>
                <th>存檔時間</th>
                <th>標的股價</th>
                <th>理論價值</th>
                <th>建議避險張數</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="historyLogs.length === 0">
                <td colspan="4" style="text-align: center; color: #888;">尚無紀錄</td>
              </tr>
              <!-- 表格資料對應 JSON 的首字大寫名稱 -->
              <tr v-for="log in historyLogs" :key="log.Log_ID">
                <td>{{ new Date(log.Created_Time).toLocaleString() }}</td>
                <td>{{ log.Market_Price }}</td>
                <td class="text-blue">{{ log.Theory_Price }}</td>
                <td class="text-red" style="font-weight: bold;">{{ log.Hedge_Qty }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>

    </div>

    <!-- Toast 提示 -->
    <div v-if="toastMessage" class="toast">
      {{ toastMessage }}
    </div>

  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

const warrants = ref([])
const filteredWarrants = ref([])
const searchKeyword = ref('')
const selectedWarrant = ref(null)

const marketPrice = ref(0)
const theoryPrice = ref(0)
const hedgeQty = ref(0)
const historyLogs = ref([])
const toastMessage = ref('')

const fetchWarrants = async () => {
  try {
    const res = await axios.get(`/api/warrant/list`, {
      params: { keyword: searchKeyword.value }
    })
    warrants.value = res.data
    filteredWarrants.value = res.data
  } catch (err) {
    showToast('無法取得清單，請檢查後端 API')
  }
}

// 點擊選擇權證
const selectWarrant = (w) => {
  // 防呆：確保物件存在且有代碼
  if (!w || (!w.warrant_ID && !w.Warrant_ID)) {
    console.error('選擇的權證物件無效', w)
    return
  }

  selectedWarrant.value = w
  marketPrice.value = 0
  theoryPrice.value = 0
  hedgeQty.value = 0

  // 統一取得正確大小寫的 ID (防範 SQL 欄位大小寫差異)
  const warrantId = w.warrant_ID || w.Warrant_ID
  fetchHistory(warrantId)
}

const handleCalculate = async (isSave) => {
  if (marketPrice.value <= 0) {
    if (isSave) alert('防禦性檢查：標的股價必須大於 0，禁止存檔！')
    theoryPrice.value = 0
    hedgeQty.value = 0
    return
  }

  try {
    const res = await axios.post('/api/warrant/calculate', {
      warrant_ID: selectedWarrant.value.warrant_ID,
      market_Price: marketPrice.value,
      save_Flag: isSave
    })

    theoryPrice.value = res.data.theory_Price
    hedgeQty.value = res.data.hedge_Qty

    if (isSave) {
      showToast('儲存試算結果成功！')
      fetchHistory(selectedWarrant.value.warrant_ID)
    }
  } catch (err) {
    alert(err.response?.data?.message || '計算錯誤')
  }
}

const fetchHistory = async (warrantId) => {
  if (!warrantId) return; // 如果 ID 是空的或 undefined 就直接返回，不發送請求

  try {
    const res = await axios.get(`/api/warrant/history/${warrantId}`)
    historyLogs.value = res.data
  } catch (err) {
    console.error('取得歷史紀錄失敗', err)
  }
}

const showToast = (msg) => {
  toastMessage.value = msg
  setTimeout(() => { toastMessage.value = '' }, 3000)
}

onMounted(() => {
  fetchWarrants()
})
</script>