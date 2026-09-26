-- ============================================
-- BASE DE DATOS: CAFÉ AROMA
-- Basado en el diagrama ER de Alex (Página-10 del .drawio)
-- Correcciones y tablas faltantes agregadas por Arnold (Integrante 3 — Backend/BD)
-- ============================================

-- 1. USUARIO
-- CORRECCIÓN: rol ahora usa CHECK para evitar valores inconsistentes
-- ("Dueno", "dueño", "DUENO" quedarían como el mismo valor "dueno")
CREATE TABLE usuario (
    usuario_id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    rol VARCHAR(20) NOT NULL CHECK (rol IN ('dueno', 'encargada'))
);

-- 2. CLIENTE
CREATE TABLE cliente (
    cliente_id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    telefono VARCHAR(20)
);

-- 3. CATEGORÍA
CREATE TABLE categoria (
    categoria_id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL
);

-- 4. PRODUCTO
-- NOTA SIN CORREGIR (hablar con Alex): este "stock" en producto puede
-- desincronizarse con el stock real de insumos que se calcula vía
-- producto_insumo + insumo.stock_actual. Si un producto se arma con
-- insumos (ej. hamburguesa), su disponibilidad ya se puede derivar del
-- insumo más escaso, no debería llevar su propio contador. Lo dejamos
-- por ahora tal como lo diseñó Alex, pero hay que decidirlo en equipo.
CREATE TABLE producto (
    producto_id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    precio NUMERIC(10,2) NOT NULL CHECK (precio >= 0),
    stock NUMERIC(10,2) NOT NULL DEFAULT 0,
    categoria_id INTEGER NOT NULL,

    CONSTRAINT fk_producto_categoria
        FOREIGN KEY (categoria_id)
        REFERENCES categoria(categoria_id)
);

-- 5. INSUMO
-- CORRECCIÓN: se agrega stock_minimo, necesario para poder comparar
-- contra stock_actual y saber cuándo generar una alerta (RF04 / HU-06)
CREATE TABLE insumo (
    insumo_id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    stock_actual NUMERIC(10,2) NOT NULL DEFAULT 0 CHECK (stock_actual >= 0),
    stock_minimo NUMERIC(10,2) NOT NULL DEFAULT 0 CHECK (stock_minimo >= 0)
);

-- 6. PRODUCTO_INSUMO
CREATE TABLE producto_insumo (
    producto_id INTEGER NOT NULL,
    insumo_id INTEGER NOT NULL,
    cantidad_necesaria NUMERIC(10,2) NOT NULL CHECK (cantidad_necesaria > 0),

    PRIMARY KEY (producto_id, insumo_id),

    CONSTRAINT fk_producto_insumo_producto
        FOREIGN KEY (producto_id)
        REFERENCES producto(producto_id),

    CONSTRAINT fk_producto_insumo_insumo
        FOREIGN KEY (insumo_id)
        REFERENCES insumo(insumo_id)
);

-- 7. VENTA
-- CORRECCIÓN: cliente_id ahora es opcional (NULL permitido). La mayoría
-- de las ventas son de mostrador sin pedir datos del cliente, según la
-- entrevista. Obligarlo forzaría a inventar un cliente en cada venta.
CREATE TABLE venta (
    venta_id SERIAL PRIMARY KEY,
    fecha_hora TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    total NUMERIC(10,2) NOT NULL CHECK (total >= 0),
    usuario_id INTEGER NOT NULL,
    cliente_id INTEGER,

    CONSTRAINT fk_venta_usuario
        FOREIGN KEY (usuario_id)
        REFERENCES usuario(usuario_id),

    CONSTRAINT fk_venta_cliente
        FOREIGN KEY (cliente_id)
        REFERENCES cliente(cliente_id)
);

-- 8. DETALLE_VENTA
CREATE TABLE detalle_venta (
    detalle_id SERIAL PRIMARY KEY,
    cantidad INTEGER NOT NULL CHECK (cantidad > 0),
    precio_unitario NUMERIC(10,2) NOT NULL CHECK (precio_unitario >= 0),
    venta_id INTEGER NOT NULL,
    producto_id INTEGER NOT NULL,

    CONSTRAINT fk_detalle_venta
        FOREIGN KEY (venta_id)
        REFERENCES venta(venta_id),

    CONSTRAINT fk_detalle_producto
        FOREIGN KEY (producto_id)
        REFERENCES producto(producto_id)
);

-- 9. MOVIMIENTO_INVENTARIO
-- CORRECCIÓN: se agrega fecha (para saber cuándo ocurrió cada movimiento)
-- y usuario_id (para saber quién lo hizo), y un CHECK sobre "tipo"
CREATE TABLE movimiento_inventario (
    movimiento_id SERIAL PRIMARY KEY,
    tipo VARCHAR(20) NOT NULL CHECK (tipo IN ('entrada', 'salida', 'merma', 'ajuste')),
    cantidad NUMERIC(10,2) NOT NULL CHECK (cantidad > 0),
    insumo_id INTEGER NOT NULL,
    usuario_id INTEGER,
    fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_movimiento_insumo
        FOREIGN KEY (insumo_id)
        REFERENCES insumo(insumo_id),

    CONSTRAINT fk_movimiento_usuario
        FOREIGN KEY (usuario_id)
        REFERENCES usuario(usuario_id)
);

-- 10. ALERTA_STOCK
-- CORRECCIÓN: se agrega fecha (para saber cuándo se generó la alerta)
CREATE TABLE alerta_stock (
    alerta_id SERIAL PRIMARY KEY,
    nivel_detectado NUMERIC(10,2) NOT NULL,
    atendida BOOLEAN NOT NULL DEFAULT FALSE,
    insumo_id INTEGER NOT NULL,
    fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_alerta_insumo
        FOREIGN KEY (insumo_id)
        REFERENCES insumo(insumo_id)
);

-- 11. NOTA_PEDIDO
-- CORRECCIÓN IMPORTANTE: se agrega venta_id. Sin esto no había forma de
-- saber qué productos preparar en cocina para una nota (RF02 dice que la
-- nota se genera "a partir de la venta"). Se quita cliente_id porque ya
-- es alcanzable a través de venta -> cliente, evita datos duplicados.
-- También se agrega CHECK sobre "estado".
CREATE TABLE nota_pedido (
    nota_pedido_id SERIAL PRIMARY KEY,
    fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    estado VARCHAR(20) NOT NULL DEFAULT 'pendiente'
        CHECK (estado IN ('pendiente', 'en_preparacion', 'entregado')),
    venta_id INTEGER NOT NULL UNIQUE,

    CONSTRAINT fk_nota_pedido_venta
        FOREIGN KEY (venta_id)
        REFERENCES venta(venta_id)
);

-- ============================================
-- TABLAS QUE FALTABAN EN EL DIAGRAMA DE ALEX
-- (historias de Arnold: HU-20, HU-21, HU-22, HU-26, y RF10)
-- ============================================

-- 12. HISTORICO_PRECIO (RF10)
CREATE TABLE historico_precio (
    historico_precio_id SERIAL PRIMARY KEY,
    producto_id INTEGER NOT NULL,
    precio_anterior NUMERIC(10,2) NOT NULL,
    precio_nuevo NUMERIC(10,2) NOT NULL,
    usuario_id INTEGER,
    fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_historico_producto
        FOREIGN KEY (producto_id)
        REFERENCES producto(producto_id),

    CONSTRAINT fk_historico_usuario
        FOREIGN KEY (usuario_id)
        REFERENCES usuario(usuario_id)
);

-- 13. CIERRE_CAJA (HU-20 Cierre, HU-21 Resumen, HU-22 Historial)
CREATE TABLE cierre_caja (
    cierre_caja_id SERIAL PRIMARY KEY,
    fecha DATE NOT NULL DEFAULT CURRENT_DATE,
    usuario_id INTEGER NOT NULL,
    total_ventas NUMERIC(10,2) NOT NULL,
    efectivo_contado NUMERIC(10,2) NOT NULL,
    diferencia NUMERIC(10,2) GENERATED ALWAYS AS (efectivo_contado - total_ventas) STORED,
    fecha_hora TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_cierre_usuario
        FOREIGN KEY (usuario_id)
        REFERENCES usuario(usuario_id)
);

-- 14. BITACORA (HU-26)
CREATE TABLE bitacora (
    bitacora_id SERIAL PRIMARY KEY,
    usuario_id INTEGER,
    accion VARCHAR(50) NOT NULL,
    entidad_afectada VARCHAR(50) NOT NULL,
    entidad_id INTEGER,
    detalle TEXT,
    fecha_hora TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_bitacora_usuario
        FOREIGN KEY (usuario_id)
        REFERENCES usuario(usuario_id)
);

-- ============================================
-- ÍNDICES
-- ============================================
CREATE INDEX idx_venta_fecha ON venta (fecha_hora);
CREATE INDEX idx_detalle_venta_venta ON detalle_venta (venta_id);
CREATE INDEX idx_bitacora_fecha ON bitacora (fecha_hora);
CREATE INDEX idx_insumo_stock ON insumo (stock_actual, stock_minimo);
CREATE INDEX idx_movimiento_insumo ON movimiento_inventario (insumo_id);

-- ============================================
-- TRIGGER 1: al vender un producto, se generan movimientos de salida
-- de cada insumo que usa, se actualiza el stock, y si queda por debajo
-- del mínimo se genera una alerta (RF16 / HU-10, RF04 / HU-06)
-- ============================================
CREATE OR REPLACE FUNCTION fn_descontar_stock()
RETURNS TRIGGER AS $$
DECLARE
    rec RECORD;
    v_usuario_id INTEGER;
BEGIN
    SELECT usuario_id INTO v_usuario_id FROM venta WHERE venta_id = NEW.venta_id;

    FOR rec IN
        SELECT insumo_id, cantidad_necesaria * NEW.cantidad AS cantidad_a_descontar
        FROM producto_insumo
        WHERE producto_id = NEW.producto_id
    LOOP
        -- registrar el movimiento de salida
        INSERT INTO movimiento_inventario (tipo, cantidad, insumo_id, usuario_id)
        VALUES ('salida', rec.cantidad_a_descontar, rec.insumo_id, v_usuario_id);

        -- actualizar el stock del insumo
        UPDATE insumo
        SET stock_actual = stock_actual - rec.cantidad_a_descontar
        WHERE insumo_id = rec.insumo_id;

        -- generar alerta si quedó por debajo del mínimo y no hay una sin atender
        INSERT INTO alerta_stock (nivel_detectado, insumo_id)
        SELECT stock_actual, insumo_id
        FROM insumo
        WHERE insumo_id = rec.insumo_id
          AND stock_actual <= stock_minimo
          AND NOT EXISTS (
              SELECT 1 FROM alerta_stock
              WHERE insumo_id = rec.insumo_id AND atendida = FALSE
          );
    END LOOP;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_descontar_stock
AFTER INSERT ON detalle_venta
FOR EACH ROW
EXECUTE FUNCTION fn_descontar_stock();

-- ============================================
-- TRIGGER 2: registrar en bitácora e histórico cada cambio de precio (RF10, RF26)
-- ============================================
CREATE OR REPLACE FUNCTION fn_historico_precio()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.precio <> OLD.precio THEN
        INSERT INTO historico_precio (producto_id, precio_anterior, precio_nuevo)
        VALUES (NEW.producto_id, OLD.precio, NEW.precio);

        INSERT INTO bitacora (accion, entidad_afectada, entidad_id, detalle)
        VALUES ('CAMBIO_PRECIO', 'producto', NEW.producto_id,
                'Precio cambiado de ' || OLD.precio || ' a ' || NEW.precio);
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_historico_precio
AFTER UPDATE ON producto
FOR EACH ROW
EXECUTE FUNCTION fn_historico_precio();

-- ============================================
-- DATOS DE PRUEBA
-- ============================================
INSERT INTO usuario (nombre, rol) VALUES
('Don Mario', 'dueno'),
('Encargada Turno', 'encargada');

INSERT INTO categoria (nombre) VALUES ('Bebidas'), ('Comida'), ('Panadería');

INSERT INTO producto (nombre, precio, stock, categoria_id) VALUES
('Café Americano', 12.00, 0, 1),
('Hamburguesa Clásica', 25.00, 0, 2),
('Croissant', 8.00, 20, 3);

INSERT INTO insumo (nombre, stock_actual, stock_minimo) VALUES
('Café molido', 5, 1),
('Pan de hamburguesa', 40, 10),
('Carne molida', 3, 1);

INSERT INTO producto_insumo (producto_id, insumo_id, cantidad_necesaria) VALUES
(1, 1, 0.02),
(2, 2, 1),
(2, 3, 0.15);

-- ============================================
-- Fin del script
-- ============================================
