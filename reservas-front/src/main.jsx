import React from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App.jsx'
import 'hotel-ui/theme.css'
import './styles.css'
import { initSession, logout } from './auth.js'

// Se pregunta a auth-api quién es el usuario antes de dibujar; sin sesión se vuelve al login
initSession().then((haySesion) => {
  if (!haySesion) return logout()
  createRoot(document.getElementById('root')).render(
    <BrowserRouter><App /></BrowserRouter>
  )
})
