import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import axios from 'axios'
import './style.css'

axios.defaults.baseURL = 'http://localhost:5008'

const app = createApp(App)

app.use(router)

app.mount('#app')
