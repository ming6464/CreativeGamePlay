using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public static class DotsEX
{
    /// <summary>
    /// Tạo thành phần LocalToWorld từ vị trí, xoay, và tỷ lệ.
    /// </summary>
    /// <param name="position">Vị trí của thành phần.</param>
    /// <param name="rotation">Xoay của thành phần.</param>
    /// <param name="scale">Tỷ lệ của thành phần.</param>
    /// <returns>LocalToWorld được tạo.</returns>
    public static LocalToWorld GetComponentWorldTf(float3 position, quaternion rotation, float scale)
    {
        return new LocalToWorld()
        {
            Value = MathExt.TRSToMatrix(position, rotation, new float3(1, 1, 1) * scale),
        };
    }

    /// <summary>
    /// Tạo thành phần LocalTransform từ vị trí, xoay, và tỷ lệ.
    /// </summary>
    /// <param name="position">Vị trí của thành phần.</param>
    /// <param name="rotation">Xoay của thành phần.</param>
    /// <param name="scale">Tỷ lệ của thành phần.</param>
    /// <returns>LocalTransform được tạo.</returns>
    public static LocalTransform GetComponentLocalTf(float3 position, quaternion rotation, float scale)
    {
        return new LocalTransform()
        {
            Position = position,
            Rotation = rotation,
            Scale = scale,
        };
    }

    /// <summary>
    /// Chuyển đổi dữ liệu từ LocalTransform sang LocalToWorld.
    /// </summary>
    /// <param name="lt">LocalTransform cần chuyển đổi.</param>
    /// <returns>LocalToWorld sau khi chuyển đổi.</returns>
    public static LocalToWorld ConvertDataLocalToWorldTf(LocalTransform lt)
    {
        return GetComponentWorldTf(lt.Position, lt.Rotation, lt.Scale);
    }

    /// <summary>
    /// Chuyển đổi dữ liệu từ LocalToWorld sang LocalTransform.
    /// </summary>
    /// <param name="ltw">LocalToWorld cần chuyển đổi.</param>
    /// <returns>LocalTransform sau khi chuyển đổi.</returns>
    public static LocalTransform ConvertDataWorldToLocalTf(LocalToWorld ltw)
    {
        return GetComponentLocalTf(ltw.Position, ltw.Rotation, ltw.Value.c0.x);
    }

    /// <summary>
    /// Tạo một LocalTransform mặc định với Position là float3.zero, Scale là 1, và Rotation là quaternion.identity.
    /// </summary>
    /// <returns>LocalTransform mặc định.</returns>
    public static LocalTransform LocalTransformDefault()
    {
        return new LocalTransform()
        {
            Position = float3.zero,
            Scale = 1,
            Rotation = quaternion.identity,
        };
    }

    /// <summary>
    /// Tạo một LocalToWorld mặc định với Position là float3.zero, Rotation là quaternion.identity, và Scale là 1.
    /// </summary>
    /// <returns>LocalToWorld mặc định.</returns>
    public static LocalToWorld LocalToWorldDefault()
    {
        return DotsEX.GetComponentWorldTf(float3.zero, quaternion.identity, 1);
    }

    /// <summary>
    /// Thêm các component LocalToWorld và LocalTransform mặc định vào một entity thông qua EntityCommandBuffer.
    /// </summary>
    /// <param name="ecb">EntityCommandBuffer được sử dụng để thêm component.</param>
    /// <param name="entity">Entity cần thêm các component.</param>
    public static void AddTransformDefault(ref EntityCommandBuffer ecb, Entity entity)
    {
        ecb.AddComponent<LocalToWorld>(entity);
        ecb.AddComponent(entity, LocalTransformDefault());
    }
}
public static class MathExt
{
    /// <summary>
    /// Chuyển đổi float3 thành quaternion.
    /// </summary>
    /// <param name="euler">Góc Euler cần chuyển đổi.</param>
    /// <returns>Quaternion được tạo từ góc Euler.</returns>
    public static quaternion Float3ToQuaternion(float3 euler)
    {
        return quaternion.EulerXYZ(math.radians(euler));
    }

    /// <summary>
    /// Chuyển đổi quaternion thành float3.
    /// </summary>
    /// <param name="q">Quaternion cần chuyển đổi.</param>
    /// <returns>Góc Euler được tạo từ quaternion.</returns>
    public static float3 QuaternionToFloat3(quaternion q)
    {
        return math.degrees(ToEulerAngles(q));
    }

    /// <summary>
    /// Chuyển đổi quaternion thành góc Euler.
    /// </summary>
    /// <param name="q">Quaternion cần chuyển đổi.</param>
    /// <returns>Góc Euler được tạo từ quaternion.</returns>
    private static float3 ToEulerAngles(quaternion q)
    {
        float3 angles;

        // Roll (xoay trục x)
        float sinr_cosp = 2 * (q.value.w * q.value.x + q.value.y * q.value.z);
        float cosr_cosp = 1 - 2 * (q.value.x * q.value.x + q.value.y * q.value.y);
        angles.x = math.atan2(sinr_cosp, cosr_cosp);

        // Pitch (xoay trục y)
        float sinp = 2 * (q.value.w * q.value.y - q.value.z * q.value.x);
        if (math.abs(sinp) >= 1)
            angles.y = math.sign(sinp) * (math.PI / 2); // sử dụng 90 độ nếu ngoài phạm vi
        else
            angles.y = math.asin(sinp);

        // Yaw (xoay trục z)
        float siny_cosp = 2 * (q.value.w * q.value.z + q.value.x * q.value.y);
        float cosy_cosp = 1 - 2 * (q.value.y * q.value.y + q.value.z * q.value.z);
        angles.z = math.atan2(siny_cosp, cosy_cosp);

        return angles;
    }

    /// <summary>
    /// Tạo một ma trận từ vị trí, xoay và tỷ lệ.
    /// </summary>
    /// <param name="position">Vị trí của thành phần.</param>
    /// <param name="rotation">Xoay của thành phần.</param>
    /// <param name="scale">Tỷ lệ của thành phần.</param>
    /// <returns>Ma trận 4x4 được tạo từ vị trí, xoay và tỷ lệ.</returns>
    public static float4x4 TRSToMatrix(float3 position, quaternion rotation, float3 scale)
    {
        return float4x4.TRS(position, rotation, scale);
    }

    /// <summary>
    /// Chuyển đổi ma trận thành vị trí, xoay và tỷ lệ.
    /// </summary>
    /// <param name="matrix">Ma trận cần chuyển đổi.</param>
    /// <param name="position">Vị trí được trích xuất từ ma trận.</param>
    /// <param name="rotation">Xoay được trích xuất từ ma trận.</param>
    /// <param name="scale">Tỷ lệ được trích xuất từ ma trận.</param>
    public static void MatrixToTRS(float4x4 matrix, out float3 position, out quaternion rotation, out float3 scale)
    {
        position = matrix.c3.xyz;
        rotation = quaternion.LookRotationSafe(matrix.c2.xyz, matrix.c1.xyz);
        scale = new float3(math.length(matrix.c0.xyz), math.length(matrix.c1.xyz), math.length(matrix.c2.xyz));
    }
    

    #region Random

    /// <summary>
    /// Lấy giá trị ngẫu nhiên trong phạm vi cho trước.
    /// </summary>
    /// <param name="random">Đối tượng Random để lấy giá trị ngẫu nhiên.</param>
    /// <param name="min">Giá trị nhỏ nhất trong phạm vi.</param>
    /// <param name="max">Giá trị lớn nhất trong phạm vi.</param>
    /// <returns>Giá trị ngẫu nhiên trong phạm vi cho trước.</returns>
    public static int GetRandomRange(this Random random, int min, int max)
    {
        return random.NextInt(min, max);
    }

    /// <summary>
    /// Lấy giá trị float3 ngẫu nhiên trong phạm vi cho trước.
    /// </summary>
    /// <param name="random">Đối tượng Random để lấy giá trị ngẫu nhiên.</param>
    /// <param name="min">Giá trị float3 nhỏ nhất trong phạm vi.</param>
    /// <param name="max">Giá trị float3 lớn nhất trong phạm vi.</param>
    /// <returns>Giá trị float3 ngẫu nhiên trong phạm vi cho trước.</returns>
    public static float3 GetRandomRange(this Random random, float3 min, float3 max)
    {
        return random.NextFloat3(min, max);
    }

    /// <summary>
    /// Lấy giá trị float2 ngẫu nhiên trong phạm vi cho trước.
    /// </summary>
    /// <param name="random">Đối tượng Random để lấy giá trị ngẫu nhiên.</param>
    /// <param name="min">Giá trị float2 nhỏ nhất trong phạm vi.</param>
    /// <param name="max">Giá trị float2 lớn nhất trong phạm vi.</param>
    /// <returns>Giá trị float2 ngẫu nhiên trong phạm vi cho trước.</returns>
    public static float2 GetRandomRange(this Random random, float2 min, float2 max)
    {
        return random.NextFloat2(min, max);
    }

    /// <summary>
    /// Lấy giá trị float ngẫu nhiên trong phạm vi cho trước.
    /// </summary>
    /// <param name="random">Đối tượng Random để lấy giá trị ngẫu nhiên.</param>
    /// <param name="min">Giá trị float nhỏ nhất trong phạm vi.</param>
    /// <param name="max">Giá trị float lớn nhất trong phạm vi.</param>
    /// <returns>Giá trị float ngẫu nhiên trong phạm vi cho trước.</returns>
    public static float GetRandomRange(this Random random, float min, float max)
    {
        return random.NextFloat(min, max);
    }

    /// <summary>
    /// Lấy giá trị ngẫu nhiên trong phạm vi cho trước.
    /// </summary>
    /// <param name="min">Giá trị nhỏ nhất trong phạm vi.</param>
    /// <param name="max">Giá trị lớn nhất trong phạm vi.</param>
    /// <returns>Giá trị ngẫu nhiên trong phạm vi cho trước.</returns>
    public static int GetRandomRange(int min, int max)
    {
        return GetRandomProperty(GetSeedWithTime()).NextInt(min, max);
    }

    /// <summary>
    /// Lấy giá trị float3 ngẫu nhiên từ seed và phạm vi cho trước.
    /// </summary>
    /// <param name="seed">Seed để tạo giá trị ngẫu nhiên.</param>
    /// <param name="min">Giá trị float3 nhỏ nhất trong phạm vi.</param>
    /// <param name="max">Giá trị float3 lớn nhất trong phạm vi.</param>
    /// <returns>Giá trị float3 ngẫu nhiên từ seed và phạm vi cho trước.</returns>
    public static float3 GetRandomRange(uint seed, float3 min, float3 max)
    {
        return GetRandomProperty(seed).NextFloat3(min, max);
    }

    /// <summary>
    /// Lấy giá trị float3 ngẫu nhiên trong phạm vi cho trước.
    /// </summary>
    /// <param name="min">Giá trị float3 nhỏ nhất trong phạm vi.</param>
    /// <param name="max">Giá trị float3 lớn nhất trong phạm vi.</param>
    /// <returns>Giá trị float3 ngẫu nhiên trong phạm vi cho trước.</returns>
    public static float3 GetRandomRange(float3 min, float3 max)
    {
        return GetRandomProperty(GetSeedWithTime()).NextFloat3(min, max);
    }

    /// <summary>
    /// Lấy giá trị float2 ngẫu nhiên trong phạm vi cho trước.
    /// </summary>
    /// <param name="min">Giá trị float2 nhỏ nhất trong phạm vi.</param>
    /// <param name="max">Giá trị float2 lớn nhất trong phạm vi.</param>
    /// <returns>Giá trị float2 ngẫu nhiên trong phạm vi cho trước.</returns>
    public static float2 GetRandomRange(float2 min, float2 max)
    {
        return GetRandomProperty(GetSeedWithTime()).NextFloat2(min, max);
    }

    /// <summary>
    /// Lấy giá trị float ngẫu nhiên trong phạm vi cho trước.
    /// </summary>
    /// <param name="min">Giá trị float nhỏ nhất trong phạm vi.</param>
    /// <param name="max">Giá trị float lớn nhất trong phạm vi.</param>
    /// <returns>Giá trị float ngẫu nhiên trong phạm vi cho trước.</returns>
    public static float GetRandomRange(float min, float max)
    {
        return GetRandomProperty(GetSeedWithTime()).NextFloat(min, max);
    }

    /// <summary>
    /// Tạo một Random từ seed.
    /// </summary>
    /// <param name="seed">Seed để tạo đối tượng Random.</param>
    /// <returns>Đối tượng Random được tạo từ seed.</returns>
    private static Random GetRandomProperty(uint seed)
    {
        return Random.CreateFromIndex(seed);
    }

    /// <summary>
    /// Lấy seed từ thời gian hiện tại.
    /// </summary>
    /// <returns>Seed được lấy từ thời gian hiện tại.</returns>
    public static uint GetSeedWithTime()
    {
        long tick = GetTimeTick();

        if (tick > 255)
        {
            tick %= 255;
        }

        return (uint)tick;
    }

    /// <summary>
    /// Lấy số tick của thời gian hiện tại.
    /// </summary>
    /// <returns>Số tick của thời gian hiện tại.</returns>
    public static long GetTimeTick()
    {
        return System.DateTime.Now.Ticks;
    }

    #endregion

    #region Interpolate
    
    /// <summary>
    /// Di chuyển float3 từ vị trí hiện tại đến mục tiêu với một bước nhất định.
    /// </summary>
    /// <param name="current">Vị trí hiện tại.</param>
    /// <param name="target">Vị trí mục tiêu.</param>
    /// <param name="step">Bước di chuyển tối đa cho mỗi lần cập nhật.</param>
    /// <returns>float3 mới sau khi di chuyển.</returns>
    public static float3 MoveTowards(float3 current, float3 target, float step)
    {
        float3 direction = target - current;
        float distance = math.length(direction);
        if (distance <= step || distance == 0f)
            return target;
        return current + math.normalize(direction) * step;
    }
    
    /// <summary>
    /// Di chuyển float2 từ vị trí hiện tại đến mục tiêu với một bước nhất định.
    /// </summary>
    /// <param name="current">Vị trí hiện tại.</param>
    /// <param name="target">Vị trí mục tiêu.</param>
    /// <param name="step">Bước di chuyển tối đa cho mỗi lần cập nhật.</param>
    /// <returns>float2 mới sau khi di chuyển.</returns>
    public static float2 MoveTowards(float2 current, float2 target, float step)
    {
        float2 direction = target - current;
        float distance = math.length(direction);
        if (distance <= step || distance == 0f)
            return target;
        return current + math.normalize(direction) * step;
    }
    
    /// <summary>
    /// Di chuyển quaternion từ giá trị hiện tại đến mục tiêu với một góc quay nhất định.
    /// </summary>
    /// <param name="current">Quaternion hiện tại.</param>
    /// <param name="target">Quaternion mục tiêu.</param>
    /// <param name="maxDegreesDelta">Góc quay tối đa cho mỗi lần cập nhật.</param>
    /// <returns>quaternion mới sau khi quay.</returns>
    public static quaternion MoveTowards(quaternion current, quaternion target, float maxDegreesDelta)
    {
        float angle = math.degrees(math.angle(current, target));
        if (angle <= maxDegreesDelta)
            return target;
        return math.slerp(current, target, maxDegreesDelta / angle);
    }
    
    /// <summary>
    /// Di chuyển int từ giá trị hiện tại đến mục tiêu với một bước nhất định.
    /// </summary>
    /// <param name="current">Giá trị hiện tại.</param>
    /// <param name="target">Giá trị mục tiêu.</param>
    /// <param name="step">Bước di chuyển tối đa cho mỗi lần cập nhật.</param>
    /// <returns>int mới sau khi di chuyển.</returns>
    public static int MoveTowards(int current, int target, int step)
    {
        int difference = target - current;
        if (math.abs(difference) <= step)
            return target;
        return current + (int)math.sign(difference) * step;
    }

    /// <summary>
    /// Di chuyển float từ giá trị hiện tại đến mục tiêu với một bước nhất định.
    /// </summary>
    /// <param name="current">Giá trị hiện tại.</param>
    /// <param name="target">Giá trị mục tiêu.</param>
    /// <param name="step">Bước di chuyển tối đa cho mỗi lần cập nhật.</param>
    /// <returns>float mới sau khi di chuyển.</returns>
    public static float MoveTowards(float current, float target, float step)
    {
        float difference = target - current;
        if (math.abs(difference) <= step)
            return target;
        return current + math.sign(difference) * step;
    }
    
    #endregion Interpolate

    /// <summary>
    /// Tính toán góc giữa hai vector và xác định góc âm nếu vector A theo chiều kim đồng hồ so với vector B.
    /// </summary>
    /// <param name="vecA">Vector đầu tiên.</param>
    /// <param name="vecB">Vector thứ hai.</param>
    /// <returns>Góc giữa hai vector tính bằng độ, có thể là âm hoặc dương.</returns>
    public static float CalculateSignedAngle(float3 vecA, float3 vecB)
    {
        // Chuẩn hóa các vector
        vecA = math.normalize(vecA);
        vecB = math.normalize(vecB);
        
        // Tính dot product
        float dotProduct = math.dot(vecA, vecB);
        
        // Đảm bảo giá trị của dot product nằm trong khoảng [-1, 1]
        dotProduct = math.clamp(dotProduct, -1.0f, 1.0f);
        
        // Tính góc bằng hàm acos và chuyển từ radian sang độ
        float angleRadians = math.acos(dotProduct);
        float angleDegrees = math.degrees(angleRadians);
        
        // Tính cross product để xác định hướng
        float3 crossProduct = math.cross(vecA, vecB);
        
        // Xác định dấu của góc
        if (crossProduct.z < 0) // Dựa trên trục Z cho 2D vectors trong mặt phẳng XY
        {
            angleDegrees = -angleDegrees;
        }
        
        return angleDegrees;
    }

    public static float CalculateAngle(float3 vecA, float3 vecB)
    {
        // Chuẩn hóa các vector
        vecA = math.normalize(vecA);
        vecB = math.normalize(vecB);
        
        // Tính dot product
        float dotProduct = math.dot(vecA, vecB);
        
        // Đảm bảo giá trị của dot product nằm trong khoảng [-1, 1]
        dotProduct = math.clamp(dotProduct, -1.0f, 1.0f);
        
        // Tính góc bằng hàm acos và chuyển từ radian sang độ
        float angleRadians = math.acos(dotProduct);
        float angleDegrees = math.degrees(angleRadians);
        
        return angleDegrees;
    }
    
    /// <summary>
    /// So sánh hai vector float3 để kiểm tra xem chúng có bằng nhau hay không.
    /// </summary>
    /// <param name="f1">Vector float3 thứ nhất.</param>
    /// <param name="f2">Vector float3 thứ hai.</param>
    /// <returns>Trả về true nếu hai vector bằng nhau, ngược lại trả về false.</returns>
    public static bool ComparisionEqual(this float3 f1, float3 f2)
    {
        return math.all(f1 == f2);
    }
    
    
    /// <summary>
    /// So sánh hai vector float3 để kiểm tra xem chúng có bằng nhau hay không.
    /// </summary>
    /// <param name="f1">Vector float2 thứ nhất.</param>
    /// <param name="f2">Vector float2 thứ hai.</param>
    /// <returns>Trả về true nếu hai vector bằng nhau, ngược lại trả về false.</returns>
    public static bool ComparisionEqual(this float2 f1, float2 f2)
    {
        return math.all(f1 == f2);
    }
    
    
    /// <summary>
    /// Tính toán góc giữa hai vector float2.
    /// </summary>
    /// <param name="vecA">Vector đầu tiên.</param>
    /// <param name="vecB">Vector thứ hai.</param>
    /// <returns>Góc giữa hai vector tính bằng độ.</returns>
    public static float CalculateAngle(float2 vecA, float2 vecB)
    {
        // Chuẩn hóa các vector
        vecA = math.normalize(vecA);
        vecB = math.normalize(vecB);
        
        // Tính dot product
        float dotProduct = math.dot(vecA, vecB);
        
        // Đảm bảo giá trị của dot product nằm trong khoảng [-1, 1]
        dotProduct = math.clamp(dotProduct, -1.0f, 1.0f);
        
        // Tính góc bằng hàm acos và chuyển từ radian sang độ
        float angleRadians = math.acos(dotProduct);
        float angleDegrees = math.degrees(angleRadians);
        
        return angleDegrees;
    }

    /// <summary>
    /// Tính toán góc có dấu giữa hai vector float2 và xác định góc âm nếu vector A theo chiều kim đồng hồ so với vector B.
    /// </summary>
    /// <param name="vecA">Vector đầu tiên.</param>
    /// <param name="vecB">Vector thứ hai.</param>
    /// <returns>Góc có dấu giữa hai vector tính bằng độ, có thể là âm hoặc dương.</returns>
    public static float CalculateSignedAngle(float2 vecA, float2 vecB)
    {
        // Chuẩn hóa các vector
        vecA = math.normalize(vecA);
        vecB = math.normalize(vecB);
        
        // Tính dot product
        float dotProduct = math.dot(vecA, vecB);
        
        // Đảm bảo giá trị của dot product nằm trong khoảng [-1, 1]
        dotProduct = math.clamp(dotProduct, -1.0f, 1.0f);
        
        // Tính góc bằng hàm acos và chuyển từ radian sang độ
        float angleRadians = math.acos(dotProduct);
        float angleDegrees = math.degrees(angleRadians);
        
        // Tính cross product để xác định hướng
        float crossProduct = vecA.x * vecB.y - vecA.y * vecB.x;
        
        // Xác định dấu của góc
        if (crossProduct < 0)
        {
            angleDegrees = -angleDegrees;
        }
        
        return angleDegrees;
    }
    
    /// <summary>
    /// Xoay một vector theo các góc Euler (x, y, z) cung cấp.
    /// </summary>
    /// <param name="vector">Vector cần xoay.</param>
    /// <param name="rotationEuler">Góc Euler để xoay vector (đơn vị: độ).</param>
    /// <returns>Vector đã được xoay.</returns>
    public static float3 RotateVector(float3 vector, float3 rotationEuler)
    {
        // Chuyển đổi góc từ độ sang radian
        float3 rotationRadians = math.radians(rotationEuler);
        
        // Tạo quaternion từ góc Euler
        quaternion rotationQuat = quaternion.Euler(rotationRadians);
        
        // Thực hiện phép xoay vector với quaternion
        return math.mul(rotationQuat, vector);
    }
    
}