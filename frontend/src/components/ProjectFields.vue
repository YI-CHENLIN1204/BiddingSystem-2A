<script setup lang="ts">
import type { ProjectForm, SystemOptions } from '../types'

defineProps<{
  modelValue: ProjectForm
  options: SystemOptions
  includeName?: boolean
}>()

defineEmits<{ 'update:modelValue': [value: ProjectForm] }>()
</script>

<template>
  <div class="form-grid">
    <label v-if="includeName" class="field span-2">
      <span>專案名稱</span>
      <input
        :value="modelValue.projectName"
        required
        @input="$emit('update:modelValue', { ...modelValue, projectName: ($event.target as HTMLInputElement).value })"
      />
    </label>
    <label class="field">
      <span>建案類型</span>
      <select
        :value="modelValue.projectType"
        required
        @change="$emit('update:modelValue', { ...modelValue, projectType: ($event.target as HTMLSelectElement).value })"
      >
        <option value="">請選擇</option>
        <option v-for="item in options.projectTypes" :key="item" :value="item">{{ item }}</option>
      </select>
    </label>
    <label class="field">
      <span>結構類型</span>
      <select
        :value="modelValue.structureType"
        required
        @change="$emit('update:modelValue', { ...modelValue, structureType: ($event.target as HTMLSelectElement).value })"
      >
        <option value="">請選擇</option>
        <option v-for="item in options.structureTypes" :key="item" :value="item">{{ item }}</option>
      </select>
    </label>
    <label class="field">
      <span>縣市</span>
      <select
        :value="modelValue.city"
        required
        @change="$emit('update:modelValue', { ...modelValue, city: ($event.target as HTMLSelectElement).value })"
      >
        <option value="">請選擇</option>
        <option v-for="item in options.cities" :key="item" :value="item">{{ item }}</option>
      </select>
    </label>
    <label class="field">
      <span>建案年月</span>
      <input
        type="month"
        :value="modelValue.projectMonth"
        required
        @input="$emit('update:modelValue', { ...modelValue, projectMonth: ($event.target as HTMLInputElement).value })"
      />
    </label>
    <label class="field">
      <span>總樓地板面積</span>
      <input
        type="number"
        min="0"
        step="0.01"
        :value="modelValue.totalFloorArea ?? ''"
        required
        @input="$emit('update:modelValue', { ...modelValue, totalFloorArea: Number(($event.target as HTMLInputElement).value) || null })"
      />
    </label>
    <label class="field">
      <span>地上樓層</span>
      <input
        type="number"
        min="1"
        :value="modelValue.floorCount ?? ''"
        required
        @input="$emit('update:modelValue', { ...modelValue, floorCount: Number(($event.target as HTMLInputElement).value) || null })"
      />
    </label>
    <label class="field">
      <span>地下樓層</span>
      <input
        type="number"
        min="0"
        :value="modelValue.basementFloorCount ?? ''"
        required
        @input="$emit('update:modelValue', { ...modelValue, basementFloorCount: Number(($event.target as HTMLInputElement).value) })"
      />
    </label>
  </div>
</template>
