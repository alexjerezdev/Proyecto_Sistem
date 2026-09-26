# Sistema POS + Inventario — Cafetería con Hamburguesas

Sistema de punto de venta, inventario y reportes para una cafetería local que también vende hamburguesas. Proyecto de la materia Sistemas de Información 1.

## Descripción

El sistema permite registrar ventas, generar notas de pedido a cocina, controlar el stock de insumos (café, leche, pan, cárnicos, embutidos), alertar cuando algo crítico está por agotarse, hacer cierre de caja y ver reportes del día.

## Metodología

Scrum, 2 sprints de 1 semana, con prácticas de XP (revisión de código, pruebas básicas).

## Equipo

| Integrante | Rol Scrum | Módulo |
|---|---|---|
| Alex Jerez | Product Owner (PO) | Login / Cierre de caja |
| Gloria Baldiviezo | Scrum Master (SM) | Pruebas / Documentación |
| Arnold Vargas | Dev Team | Backend / Base de datos |
| Luis Huanca | Dev Team | Frontend / Pruebas Finales | 

## Stack

- Backend + Frontend: a elección del equipo
- Base de datos: PostgreSQL
- Editor:  Visual Studio Code

## Estructura del repositorio

backend/ → API y lógica del servidor
frontend/ → Interfaz de usuario
docs/ → Entrevista, RF/RNF, Product Backlog, diagramas
database/ → Scripts y diagrama entidad-relación

## Ramas

- `main` → versión estable
- `dev` → integración de features
- una rama por integrante para su trabajo diario