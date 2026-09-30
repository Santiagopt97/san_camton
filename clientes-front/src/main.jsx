import React from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App.jsx'
import 'hotel-ui/theme.css'
import './styles.css'
import { initSession, logout } from './auth.js'

if (!initSession()) logout()
else createRoot(document.getElementById('root')).render(
  <BrowserRouter><App /></BrowserRouter>
)
